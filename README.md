## Overview

An app that records audio from a device of your choice, transcribes and then sends it to a GPT to do whatever, based on a prompt you write yourself.

Prompts can be found in a language file in the Settings folder.

The general multilingual version of the program will be able to support up to 52 languages due to use of Qwen3-ASR-0.6b

## Usage

Launch the app, enable notes (or not), press dedicated button to answer a recorded question (or do whatever with text retrieved from audio, based on prompts).

Configuration is done within the app.

Exit the app when you are done. (by pressing the dedicated button!!)

## Pipeline

<img width="791" height="501" alt="attentivestudent" src="https://github.com/user-attachments/assets/0b5c3be6-6005-4b1d-9465-538467cd33d0" />

## Installation guide

1. Install Docker Desktop from https://www.docker.com.
2. Install continuumio/miniconda3:latest image from docker hub.
3. Run the app (AttentiveStudent.exe), follow the instructions and then wait for it to finish building the image. Depending on your internet speed it might take more than 30 minutes. After the setup is finished you won’t have to redo it again and it will only take a couple of seconds to launch the app.
4. Done!

## Translation guide

1. Go to Settings folder and open the languages.json file
2. Add your language to the file following the instructions present in the file.
3. Copy and translate russian.json, save it with the same full name you’d written in the languages.json file (eg. “eng”: “english” -> english.json)
4. Done!

## Models used

ai-sage/GigaAM-v3: https://huggingface.co/ai-sage/GigaAM-v3

openai/whisper-small: https://huggingface.co/openai/whisper-small
