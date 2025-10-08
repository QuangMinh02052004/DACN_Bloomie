import scipy.io as sio
import glob, numpy as np, tensorflow as tf

# Load model đã train
model = tf.keras.models.load_model("oxford102_m2_optimized.h5")

# Load test split
setid = sio.loadmat("setid.mat")
test_idx = setid["tstid"].flatten() - 1

# Chuẩn bị file list và nhãn như khi train
all_files = sorted(glob.glob("images/jpg/*.jpg"))
labels_all = sio.loadmat("imagelabels.mat")["labels"].flatten() - 1

test_files = [all_files[i] for i in test_idx]
test_labels = labels_all[test_idx]


# Tạo dataset
def parse_image(fn, lb):
    img = tf.io.read_file(fn)
    img = tf.image.decode_jpeg(img, channels=3)
    img = tf.image.resize(img, (224, 224)) / 255.0
    return img, lb


ds_test = (
    tf.data.Dataset.from_tensor_slices((test_files, test_labels))
    .map(parse_image)
    .batch(64)
)

# Đánh giá
loss, acc = model.evaluate(ds_test)
print(f"Test accuracy: {acc * 100:.2f}%")
