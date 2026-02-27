import torch
from transformers import AutoModelForSpeechSeq2Seq, AutoProcessor, pipeline

class Transcriber:
    def __init__(self):
        device = "cpu"
        torch_dtype = torch.float32
        model_path = "app/whisper-small"

        model = AutoModelForSpeechSeq2Seq.from_pretrained(
            model_path, dtype=torch_dtype, low_cpu_mem_usage=True, use_safetensors=True
        )
        model.to(device)

        processor = AutoProcessor.from_pretrained(model_path)
        self._pipe = pipeline(
            "automatic-speech-recognition",
            model=model,
            tokenizer=processor.tokenizer,
            feature_extractor=processor.feature_extractor,
            torch_dtype=torch_dtype,
            device=device,
            ignore_warning=True
            )

        with open("app/language.txt", "r") as f:
            self._language = f.readline()
        print("Transcriber setup successful!")
            
    def process_audio(self, audiofiles, duration: int):
        print("Transcribing audio...")
        gen_kwargs = {
            "condition_on_prev_tokens": False,
            "compression_ratio_threshold": 1.35,  # zlib compression ratio threshold (in token space)
            "logprob_threshold": -1.0,
            "no_speech_threshold": 0.6,
            "temperature": (0.5, 0.9),
            "return_timestamps": True,
            "language": self._language
        }
        return self._pipe(audiofiles, generate_kwargs=gen_kwargs, 
                                          batch_size=1, 
                                          chunk_length_s=duration)["text"]