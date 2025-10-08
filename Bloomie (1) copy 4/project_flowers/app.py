import os
import json
import numpy as np
from flask import Flask, request, jsonify
from PIL import Image
import tensorflow as tf

# ====== Config ======
MODEL_PATH = "oxford102_m2_optimized.h5"
CLASS_MAP_PATH = "class_names.json"
IMG_SIZE = (224, 224)

# ====== Khởi tạo app ======
app = Flask(__name__)

# Load mapping id → tên hoa
with open(CLASS_MAP_PATH, "r", encoding="utf-8") as f:
    CLASS_NAME_MAP = json.load(f)

# Load model một lần khi khởi server
model = tf.keras.models.load_model(MODEL_PATH)


# ====== Helper ======
def preprocess_image(img: Image.Image) -> np.ndarray:
    img = img.convert("RGB").resize(IMG_SIZE)
    arr = np.array(img).astype("float32") / 255.0
    return np.expand_dims(arr, axis=0)


# ====== Endpoint ======
@app.route("/search-by-image", methods=["POST"])
def search_by_image():
    """
    Nhận form-data với key 'image', trả về JSON:
    {
      "class_id": 23,
      "class_name": "rose",
      "probability": 0.92,
      "products": [ ... ]  # placeholder
    }
    """
    if "image" not in request.files:
        return jsonify({"error": "Missing 'image' file"}), 400

    file = request.files["image"]
    try:
        img = Image.open(file.stream)
    except Exception as e:
        return jsonify({"error": "Invalid image"}), 400

    # Dự đoán
    x = preprocess_image(img)
    preds = model.predict(x)[0]
    idx = int(np.argmax(preds)) + 1  # class_id 1..102
    prob = float(np.max(preds))

    # Lấy tên hoa
    class_name = CLASS_NAME_MAP.get(str(idx), f"class_{idx:03d}")

    # TODO: Thay list sản phẩm thực tế bằng query DB theo class_name
    products = [{"id": 101, "name": f"Sample bouquet for {class_name}", "price": 29.99}]

    return jsonify(
        {
            "class_id": idx,
            "class_name": class_name,
            "probability": round(prob, 4),
            "products": products,
        }
    ), 200


# ====== Chạy server ======
if __name__ == "__main__":
    # Port 8000 hoặc bất kỳ
    app.run(host="0.0.0.0", port=8000, debug=True)
