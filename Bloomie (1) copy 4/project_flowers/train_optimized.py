# train_optimized.py
import os

# 1. BẮT BUỘC: kích hoạt Metal plugin và các opt
os.environ["TF_ENABLE_ONEDNN_OPTS"] = "1"  # oneDNN cho CPU
os.environ["TF_MPS_ENABLE_FALLBACK"] = "1"  # cho Metal (Apple Silicon)
os.environ["TF_METAL_USE_UNIFIED_MEMORY"] = "1"

import glob
import scipy.io as sio
import numpy as np
import tensorflow as tf

from tensorflow.keras import layers, models, callbacks
from tensorflow.keras.utils import to_categorical
from tensorflow.keras import mixed_precision

mixed_precision.set_global_policy("mixed_float16")

# 3. Điều chỉnh threading cho CPU
tf.config.threading.set_intra_op_parallelism_threads(8)
tf.config.threading.set_inter_op_parallelism_threads(4)

# 4. Cấu hình đường dẫn & tham số
IMG_DIR = "images/jpg"
LABELS_MAT = "imagelabels.mat"
SETID_MAT = "setid.mat"
OUT_MODEL = "oxford102_m2_optimized.h5"

IMG_SIZE = (224, 224)
# Tăng batch size nếu mem cho phép, thử 64 trước
BATCH_SIZE = 64
EPOCHS = 30
AUTOTUNE = tf.data.AUTOTUNE
NUM_CLASSES = 102

# 5. Setup GPU memory growth (nếu cần)
gpus = tf.config.list_physical_devices("GPU")
if gpus:
    tf.config.experimental.set_memory_growth(gpus[0], True)

# 6. Load labels và split indices
labels_all = sio.loadmat(LABELS_MAT)["labels"].flatten()
setid = sio.loadmat(SETID_MAT)
train_idx = setid["trnid"].flatten() - 1
val_idx = setid["valid"].flatten() - 1

# 7. Lấy danh sách file, đảm bảo sort để khớp nhãn
all_files = sorted(glob.glob(os.path.join(IMG_DIR, "*.jpg")))
print("Number of images found:", len(all_files))
print("Number of labels:", labels_all.shape[0])
print("First 5 image files:", all_files[:5])
print("First 5 labels:", labels_all[:5])
assert len(all_files) == labels_all.shape[0]

train_files = [all_files[i] for i in train_idx]
train_labels = labels_all[train_idx] - 1
val_files = [all_files[i] for i in val_idx]
val_labels = labels_all[val_idx] - 1


# 8. Hàm load và augment ảnh
def parse_and_augment(filename, label):
    img = tf.io.read_file(filename)
    img = tf.io.decode_jpeg(img, channels=3)
    img = tf.image.resize(img, IMG_SIZE)
    img = tf.image.random_flip_left_right(img)
    img = tf.image.random_brightness(img, 0.2)
    img = tf.image.random_contrast(img, 0.8, 1.2)
    img = tf.cast(img, tf.float32) / 255.0
    return img, label


def parse_image(filename, label):
    img = tf.io.read_file(filename)
    img = tf.io.decode_jpeg(img, channels=3)
    img = tf.image.resize(img, IMG_SIZE)
    img = tf.cast(img, tf.float32) / 255.0
    return img, label


# 9. Xây dựng tf.data.Dataset tối ưu
def make_dataset(files, labels, is_train):
    ds = tf.data.Dataset.from_tensor_slices((files, labels))
    if is_train:
        ds = ds.shuffle(len(files))
        ds = ds.map(parse_and_augment, num_parallel_calls=AUTOTUNE)
    else:
        ds = ds.map(parse_image, num_parallel_calls=AUTOTUNE)
    ds = ds.batch(BATCH_SIZE, drop_remainder=is_train)
    ds = ds.prefetch(AUTOTUNE)
    return ds


train_ds = make_dataset(train_files, train_labels, is_train=True)
val_ds = make_dataset(val_files, val_labels, is_train=False)

# 10. Xây dựng model với MobileNetV2 backbone
base = tf.keras.applications.MobileNetV2(
    input_shape=(*IMG_SIZE, 3), include_top=False, weights="imagenet"
)
base.trainable = False  # freeze backbone

inputs = layers.Input((*IMG_SIZE, 3))
x = base(inputs, training=False)
x = layers.GlobalAveragePooling2D()(x)
x = layers.Dropout(0.3)(x)
# Kết quả float16, nhưng loss vẫn float32 nhờ mixed_precision
outputs = layers.Dense(NUM_CLASSES, activation="softmax", dtype="float32")(x)

model = models.Model(inputs, outputs)
model.compile(
    optimizer=tf.keras.optimizers.Adam(1e-4),
    loss="sparse_categorical_crossentropy",
    metrics=["accuracy"],
)

model.summary()

# 11. Callbacks lưu model tốt nhất & early stop
ckpt = callbacks.ModelCheckpoint(
    OUT_MODEL, save_best_only=True, monitor="val_accuracy", mode="max"
)
early = callbacks.EarlyStopping(
    monitor="val_accuracy", mode="max", patience=5, restore_best_weights=True
)

# 12. Train
model.fit(
    train_ds, validation_data=val_ds, epochs=EPOCHS, callbacks=[ckpt, early], verbose=1
)

print(f"Training xong, model tốt nhất lưu tại: {OUT_MODEL}")
