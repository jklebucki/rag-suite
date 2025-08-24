from flask import Flask, request, jsonify
from flask_cors import CORS
from sentence_transformers import SentenceTransformer
import torch
import logging
import os

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)

# Global model variable
model = None

def load_model():
    """Load the sentence transformer model"""
    global model
    try:
        logger.info("Loading sentence transformer model...")
        # Use a lightweight multilingual model
        model_name = os.getenv('MODEL_NAME', 'all-MiniLM-L6-v2')
        model = SentenceTransformer(model_name)
        logger.info(f"Model {model_name} loaded successfully")
        return True
    except Exception as e:
        logger.error(f"Error loading model: {e}")
        return False

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    status = "healthy" if model is not None else "unhealthy"
    return jsonify({"status": status}), 200 if model else 503

@app.route('/embed', methods=['POST'])
def embed():
    """Generate embeddings for input text"""
    global model
    
    if model is None:
        return jsonify({"error": "Model not loaded"}), 503
    
    try:
        data = request.get_json()
        if not data or 'inputs' not in data:
            return jsonify({"error": "Missing 'inputs' field"}), 400
        
        inputs = data['inputs']
        if isinstance(inputs, str):
            inputs = [inputs]
        
        # Generate embeddings
        embeddings = model.encode(inputs)
        
        # Convert to list for JSON serialization
        if len(inputs) == 1:
            embeddings = embeddings.tolist()
        else:
            embeddings = [emb.tolist() for emb in embeddings]
        
        return jsonify({
            "embeddings": embeddings,
            "model": model.get_sentence_embedding_dimension() if hasattr(model, 'get_sentence_embedding_dimension') else 384
        })
        
    except Exception as e:
        logger.error(f"Error generating embeddings: {e}")
        return jsonify({"error": str(e)}), 500

@app.route('/info', methods=['GET'])
def info():
    """Get model information"""
    if model is None:
        return jsonify({"error": "Model not loaded"}), 503
    
    return jsonify({
        "model_name": getattr(model, '_model_name', 'unknown'),
        "max_seq_length": getattr(model, 'max_seq_length', 512),
        "embedding_dimension": model.get_sentence_embedding_dimension() if hasattr(model, 'get_sentence_embedding_dimension') else 384
    })

if __name__ == '__main__':
    logger.info("Starting embedding service...")
    
    # Load model on startup
    if not load_model():
        logger.error("Failed to load model, exiting...")
        exit(1)
    
    # Start the Flask app
    app.run(host='0.0.0.0', port=8580, debug=False)
