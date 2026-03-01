from fastapi import FastAPI, UploadFile
from transcriber import Transcriber
from httpObjects import TranscribedText, Duration
import shutil
import os

audiofiles = []

duration = Duration(0)
trans = Transcriber()
app = FastAPI()

@app.post("/sendduration/")
async def set_duration(item: Duration):
    duration.amount = item.amount

@app.post("/transcribe/")
async def transcribe_audio(audiofile: UploadFile):
    if (duration.amount == 0):
        print("No duration was sent before transcription")
        return ""

    print(duration.amount)

    tempfile = audiofile.filename
    try:
        with open(tempfile, "xb") as buffer:
            shutil.copyfileobj(audiofile.file, buffer)
    except Exception as e:
        print(str(e))

    transcribed = f"{trans.process_audio(tempfile, duration.amount)} "
    os.remove(tempfile)

    print(transcribed)
    result = TranscribedText(transcribed)
    return result