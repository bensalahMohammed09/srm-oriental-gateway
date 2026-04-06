# Using a slim version of Python for a smaller footprint [cite: 18]
ARG PYTHON_VERSION
FROM python:${PYTHON_VERSION}

# Technical labels
LABEL component="OCR-Worker"
LABEL project="SRM-Oriental-Gateway"

# Install system dependencies for OCR and PDF processing [cite: 18]
RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-fra \
    poppler-utils \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Variables for the non-root user [cite: 18]
ARG SRM_USER_ID
ARG SRM_GROUP_ID

# Create a non-root user and group to run the application securely [cite: 18]
RUN groupadd -g ${SRM_GROUP_ID} srmgroup && \
    useradd -u ${SRM_USER_ID} -g srmgroup -m -s /bin/bash srm_worker

# Set the working directory inside the user's home folder [cite: 18]
WORKDIR /home/srm_worker/app

# Set ownership of the directory before switching users [cite: 19]
RUN chown -R srm_worker:srmgroup /home/srm_worker

# Switch to the non-root user
USER srm_worker

# 1. Copy requirements first to optimize build cache [cite: 18]
COPY --chown=srm_worker:srmgroup src/workers/ocr-service/requirements.txt .

# 2. Install Python dependencies
# Using --user to install in the non-root user's local directory
RUN pip install --no-cache-dir --user -r requirements.txt

# 3. Copy the actual application source code (Crucial fix)
COPY --chown=srm_worker:srmgroup src/workers/ocr-service/ .

# Ensure the local bin is in PATH for pip installed scripts [cite: 20]
ENV PATH="/home/srm_worker/.local/bin:${PATH}"

# Context variable 
ARG WORKER_ENV
ENV WORKER_ENV=${WORKER_ENV}

# Start the worker [cite: 18]
CMD ["python", "main.py"]