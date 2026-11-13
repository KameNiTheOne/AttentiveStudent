from transformers import AutoModelForRNNT, AutoProcessor
from transformers.audio_utils import load_audio

class Transcriber:
    def __init__(self):
        print("Setting up the transcriber...")
        device = "cpu"
        model_id = "nvidia/nemotron-3.5-asr-streaming-0.6b"

        self._processor = AutoProcessor.from_pretrained(model_id)
        self._model = AutoModelForRNNT.from_pretrained(model_id)
        self._model.to(device)

        print("Transcriber setup successful!")
            
    def process_audio(self, audiofile, duration: int, language: str):
        print("Transcribing audio...")
        audio = load_audio(
            audiofile,
            sampling_rate=self._processor.feature_extractor.sampling_rate,
        )
        
        inputs = self._processor(audio, sampling_rate=self._processor.feature_extractor.sampling_rate, language=language)
        inputs.to(self._model.device, dtype=self._model.dtype)
        output = self._model.generate(**inputs, return_dict_in_generate=True)

        return self._processor.decode(output.sequences, skip_special_tokens=True)[0]