from pydantic import BaseModel

class TranscribedText(BaseModel):
    text: str
    def __init__(self, _text: str):
        super().__init__(text=_text)

class Information(BaseModel):
    duration: int
    language: str
    def __init__(self, duration: int, language: str):
        super().__init__(duration=duration, language=language)