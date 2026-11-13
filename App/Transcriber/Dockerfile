FROM continuumio/miniconda3:latest

WORKDIR /code

# Set conda configuration
RUN conda config --set always_yes yes --set changeps1 no && \
    conda info -a && \
    conda config --add channels conda-forge

COPY ./app /code/app

# Create and set up the program environment
RUN conda install --quiet --freeze-installed -c main conda-pack && \
    conda create -n program python=3.11 && \
    conda run -n program pip install --no-cache-dir numpy==2.* torch==2.8.* torchaudio==2.8.* transformers==4.57.* pyannote.audio==4.0 torchcodec==0.7 hydra-core==1.3.* omegaconf==2.3.* sentencepiece tqdm && \
    conda run -n program conda install conda-forge::fastapi && \
    conda run -n program pip install --force-reinstall numpy && \
    conda run -n program conda install -y -c conda-forge "ffmpeg=7.*"

# Use conda run to execute uvicorn in the program environment
CMD ["conda", "run", "--no-capture-output", "-n", "program", "uvicorn", "program:app", "--app-dir", "app", "--host", "0.0.0.0", "--port", "8000"]