-- 1. Roles
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    label VARCHAR(50) NOT NULL UNIQUE 
);

-- 2. Users
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role_id INTEGER REFERENCES roles(id) ON DELETE SET NULL
);

-- 3. Categories
CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    label VARCHAR(50) NOT NULL
);

-- 4. Statuses
CREATE TABLE statuses (
    id SERIAL PRIMARY KEY,
    label VARCHAR(50) NOT NULL
);

-- 5. Documents (FIXED: Removed trailing comma)
CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    reference_number VARCHAR(50) NOT NULL UNIQUE,
    category_id INTEGER REFERENCES categories(id),
    status_id INTEGER REFERENCES statuses(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 6. OCR_Metadata (ADDED: Cascade delete)
CREATE TABLE ocr_metadata (
    id SERIAL PRIMARY KEY,
    document_id INTEGER REFERENCES documents(id) ON DELETE CASCADE,
    raw_text TEXT,
    confidence_score FLOAT,
    extracted_date DATE
);

-- 7. Workflows
CREATE TABLE workflows (
    id SERIAL PRIMARY KEY,
    document_id INTEGER REFERENCES documents(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id),
    action_taken VARCHAR(100),
    action_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 8. Audit Logs
CREATE TABLE audit_logs (
    id SERIAL PRIMARY KEY,
    event_type VARCHAR(100),
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Initial Data
INSERT INTO statuses (label) VALUES ('En attente'), ('En cours de traitement'), ('Validé'), ('Rejeté');
INSERT INTO categories (label) VALUES ('Facture'),('Courrier entrant'), ('Courrier sortant');