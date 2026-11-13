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
    conda run -n program pip install --no-cache-dir torch torchvision --index-url https://download.pytorch.org/whl/cpu && \
    conda run -n program conda install conda-forge::fastapi && \
    conda run -n program pip install --no-cache-dir transformers accelerate && \
    conda run -n program conda install -y -c conda-forge "ffmpeg" && \
	conda run -n program pip install --no-cache-dir transformers[audio]

# Use conda run to execute uvicorn in the program environment
CMD ["conda", "run", "--no-capture-output", "-n", "program", "uvicorn", "program:app", "--app-dir", "app", "--host", "0.0.0.0", "--port", "8000"]
