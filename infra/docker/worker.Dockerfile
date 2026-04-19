# --- BUILD ARGUMENTS ---
ARG BASE_IMAGE=python:3.11-bookworm
FROM ${BASE_IMAGE}

# --- LABELS ---
LABEL component="OCR-Worker"
LABEL project="SRM-Oriental-Gateway"

# --- BUILD-TIME ARGS (declared before USER for clarity) ---
ARG SRM_USER_ID=1000
ARG SRM_GROUP_ID=1000
ARG WORKER_ENV

# --- PYTHON CONFIGURATION ---
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    WORKER_ENV=${WORKER_ENV}

# --- SYSTEM DEPENDENCIES ---
RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-fra \
    poppler-utils \
    libgl1 \
    libglib2.0-0 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# --- SET TESSDATA_PREFIX DYNAMICALLY ---
RUN TESS_MAJOR=$(tesseract --version 2>&1 | awk 'NR==1{print $2}' | cut -d. -f1) && \
    echo "TESSDATA_PREFIX=/usr/share/tesseract-ocr/${TESS_MAJOR}/tessdata" >> /etc/environment
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata

# --- USER SETUP ---
RUN groupadd -g ${SRM_GROUP_ID} srmgroup && \
    useradd -u ${SRM_USER_ID} -g srmgroup -m -s /bin/bash srm_worker

# --- VIRTUALENV (avoids running app-owned packages as root) ---
RUN python -m venv /home/srm_worker/venv
ENV PATH="/home/srm_worker/venv/bin:$PATH"

WORKDIR /home/srm_worker/app

# --- PYTHON DEPENDENCIES (leverage layer cache) ---
COPY --chown=srm_worker:srmgroup src/workers/ocr-service/requirements.txt .

RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# --- APPLICATION CODE ---
COPY --chown=srm_worker:srmgroup src/workers/ocr-service/ .

# --- CRLF FIX & PERMISSIONS ---
RUN find . -type f -name "*.py" -exec sed -i 's/\r$//' {} + && \
    chown -R srm_worker:srmgroup /home/srm_worker/app

# --- SWITCH TO NON-ROOT USER ---
USER srm_worker

# --- HEALTHCHECK ---
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import os, sys; sys.exit(0 if os.path.exists('main.py') else 1)"

# --- ENTRYPOINT ---
CMD ["python", "main.py"]