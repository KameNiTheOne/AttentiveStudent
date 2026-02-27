from pydantic import BaseModel

class TranscribedText(BaseModel):
    text: str
    def __init__(self, _text: str):
        super().__init__(text=_text)

class Duration(BaseModel):
    amount: int
    def __init__(self, amount: int):
        super().__init__(amount=amount)