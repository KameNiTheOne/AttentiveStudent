from transformers import AutoModel

class Transcriber:
    def __init__(self):
        device = "cpu"
        model = "ai-sage/GigaAM-v3"

        revision = "e2e_rnnt"  # can be any v3 model: ssl, ctc, rnnt, e2e_ctc, e2e_rnnt
        self._model = AutoModel.from_pretrained(
            model,
            revision=revision,
            trust_remote_code=True,
        )
        self._model.to(device)

        print("Transcriber setup successful!")
            
    def process_audio(self, audiofile, duration: int, language: str):
        print("Transcribing audio...")
        return self._model.transcribe(audiofile)