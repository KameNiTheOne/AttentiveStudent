## Overview

An app that records audio from a device of your choice, transcribes and then sends it to a GPT to do whatever, based on a prompt you write yourself.

Prompts can be found in a language file in the Settings folder.

The general multilingual version of the program will be able to support up to 52 languages due to use of Qwen3-ASR-0.6b

## Usage

Launch the app, enable notes (or not), press dedicated button to answer a recorded question (or do whatever with text retrieved from audio, based on prompts).

Exit the app when you are done. (by pressing the dedicated button!!)

## Pipeline

<img width="791" height="501" alt="flowchart" src="https://github.com/user-attachments/assets/fba0e68e-c086-4e89-a0f2-0afa5a502dc2" />

## Installation guide

1. Install Docker Desktop from https://www.docker.com.
2. Install continuumio/miniconda3:latest image from docker hub.
3. Run Config.exe and follow the instructions to configure the program.
4. Run the app (AttentiveStudent.exe) and wait for it to finish building the image. Depending on your internet speed it might take 30 minutes. After the process is finished you won’t have to repeat it again and it will take around 10 seconds to launch the app.
5. Done!

## Translation guide

1. Go to Settings folder and open the languages.json file
2. Add your language to the file following the instructions present in the file.
3. Copy and translate russian.json, save it with the same full name you’d written in the languages.json file (eg. “eng”: “english” -> english.json)
4. Done!
