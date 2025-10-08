from tensorflow.keras.models import load_model
import numpy as np
from PIL import Image
import json

model = load_model("oxford102_m2_optimized.h5")
class_map = json.load(open("class_names.json"))

img = Image.open("images/jpg/image_00001.jpg").resize((224, 224))
x = np.array(img) / 255.0
pred = model.predict(x[None, ...])[0]
idx = int(np.argmax(pred)) + 1
print(f"Loài hoa: {class_map[str(idx)]}, độ tin cậy: {pred[idx - 1]:.2f}")
