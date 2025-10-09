from flask import Flask, request, jsonify
from flask_cors import CORS
import tensorflow as tf
import numpy as np
from PIL import Image
import io
import os

app = Flask(__name__)
CORS(app)

# Load model Oxford Flowers
model = tf.keras.models.load_model("Oxford102_m2_optimized.h5")

# Danh sách 102 loài hoa từ dataset Oxford Flowers
class_names = [
    "pink primrose",
    "hard-leaved pocket orchid",
    "canterbury bells",
    "sweet pea",
    "english marigold",
    "tiger lily",
    "moon orchid",
    "bird of paradise",
    "monkshood",
    "globe thistle",
    "snapdragon",
    "colt's foot",
    "king protea",
    "spear thistle",
    "yellow iris",
    "globe-flower",
    "purple coneflower",
    "peruvian lily",
    "balloon flower",
    "giant white arum lily",
    "fire lily",
    "pincushion flower",
    "fritillary",
    "red ginger",
    "grape hyacinth",
    "corn poppy",
    "prince of wales feathers",
    "stemless gentian",
    "artichoke",
    "sweet william",
    "carnation",
    "garden phlox",
    "love in the mist",
    "mexican aster",
    "alpine sea holly",
    "ruby-lipped cattleya",
    "cape flower",
    "great masterwort",
    "siam tulip",
    "lenten rose",
    "barbeton daisy",
    "daffodil",
    "sword lily",
    "poinsettia",
    "bolero deep blue",
    "wallflower",
    "marigold",
    "buttercup",
    "oxeye daisy",
    "common dandelion",
    "petunia",
    "wild pansy",
    "primula",
    "sunflower",
    "pelargonium",
    "bishop of llandaff",
    "gaura",
    "geranium",
    "orange dahlia",
    "pink-yellow dahlia",
    "cautleya spicata",
    "japanese anemone",
    "black-eyed susan",
    "silverbush",
    "californian poppy",
    "osteospermum",
    "spring crocus",
    "bearded iris",
    "windflower",
    "tree poppy",
    "gazania",
    "azalea",
    "water lily",
    "rose",
    "thorn apple",
    "morning glory",
    "passion flower",
    "lotus",
    "toad lily",
    "anthurium",
    "frangipani",
    "clematis",
    "hibiscus",
    "columbine",
    "desert-rose",
    "tree mallow",
    "magnolia",
    "cyclamen",
    "watercress",
    "canna lily",
    "hippeastrum",
    "bee balm",
    "ball moss",
    "foxglove",
    "bougainvillea",
    "camellia",
    "mallow",
    "mexican petunia",
    "bromelia",
    "blanket flower",
    "trumpet creeper",
    "blackberry lily",
]


@app.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "healthy", "model": "Oxford102_m2_optimized"})


@app.route("/search-by-image", methods=["POST"])
def search_by_image():
    try:
        if "imageFile" not in request.files:
            return jsonify({"error": "No image file"}), 400

        file = request.files["imageFile"]
        if file.filename == "":
            return jsonify({"error": "No selected file"}), 400

        # Preprocess image cho model Oxford Flowers (224x224)
        image = Image.open(io.BytesIO(file.read())).convert("RGB")
        image = image.resize((224, 224))
        image_array = np.array(image) / 255.0
        image_array = np.expand_dims(image_array, axis=0)

        # Predict
        predictions = model.predict(image_array)
        predicted_class = np.argmax(predictions[0])
        confidence = float(predictions[0][predicted_class])

        # Map tên hoa sang tiếng Việt (có thể customize)
        flower_name = class_names[predicted_class]
        vietnamese_name = map_flower_to_vietnamese(flower_name)

        return jsonify(
            {
                "class_id": int(predicted_class),
                "class_name": flower_name,
                "vietnamese_name": vietnamese_name,
                "probability": confidence,
            }
        )

    except Exception as e:
        print(f"Error in prediction: {str(e)}")
        return jsonify({"error": str(e)}), 500


def map_flower_to_vietnamese(english_name):
    """Map tên hoa từ tiếng Anh sang tiếng Việt"""
    flower_mapping = {
        "rose": "Hoa Hồng",
        "sunflower": "Hoa Hướng Dương",
        "daisy": "Hoa Cúc",
        "tulip": "Hoa Tulip",
        "lily": "Hoa Lily",
        "orchid": "Hoa Lan",
        "carnation": "Hoa Cẩm Chướng",
        "daffodil": "Hoa Thủy Tiên",
        "iris": "Hoa Diên Vĩ",
        "dahlia": "Hoa Thược Dược",
        "peony": "Hoa Mẫu Đơn",
        "chrysanthemum": "Hoa Cúc",
        "lavender": "Hoa Oải Hương",
        "hydrangea": "Hoa Cẩm Tú Cầu",
        "jasmine": "Hoa Nhài",
        "lotus": "Hoa Sen",
        "magnolia": "Hoa Mộc Lan",
        "marigold": "Hoa Vạn Thọ",
        "poppy": "Hoa Anh Túc",
        "zinnia": "Hoa Cúc Zinnia",
    }

    # Tìm tên phù hợp
    for eng_name, vi_name in flower_mapping.items():
        if eng_name in english_name.lower():
            return vi_name

    # Nếu không tìm thấy, trả về tên gốc
    return english_name


if __name__ == "__main__":
    print("Starting Oxford Flowers Recognition API...")
    print(f"Model loaded: Oxford102_m2_optimized.h5")
    print(f"Number of classes: {len(class_names)}")
    app.run(host="0.0.0.0", port=8000, debug=True)
