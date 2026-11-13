from fastapi import FastAPI, UploadFile
from transcriber import Transcriber
from httpObjects import TranscribedText, Information
import shutil
import os

audiofiles = []

information = Information(0, "")
trans = Transcriber()
app = FastAPI()

@app.post("/sendinformation/")
async def set_information(item: Information):
    information.duration = item.duration
    information.language = item.language

@app.post("/transcribe/")
async def transcribe_audio(audiofile: UploadFile):
    if (information.duration == 0):
        print("No information was sent before transcription")
        return ""

    print(information.duration)
    print(information.language)

    tempfile = audiofile.filename
    try:
        with open(tempfile, "xb") as buffer:
            shutil.copyfileobj(audiofile.file, buffer)
    except Exception as e:
        print(str(e))

    transcribed = f"{trans.process_audio(tempfile, information.duration, information.language)} "
    os.remove(tempfile)

    print(transcribed)
    result = TranscribedText(transcribed)
    return result