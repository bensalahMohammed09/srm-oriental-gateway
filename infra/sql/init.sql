-- Enable UUID extension for Guid support
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Roles
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL -- On autorise explicitement le NULL ici
);

-- 2. Users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role_id UUID REFERENCES roles(id) ON DELETE SET NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- 3. Categories
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- 4. Statuses
CREATE TABLE statuses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- 5. Documents
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    reference VARCHAR(100) NOT NULL UNIQUE,
    supplier_name VARCHAR(150),
    total_amount DECIMAL(18,2),
    status_id UUID NOT NULL REFERENCES statuses(id),
    category_id UUID REFERENCES categories(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- 6. OCR Metadata
CREATE TABLE ocr_metadata (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    key VARCHAR(100) NOT NULL,
    value TEXT NOT NULL,
    confidence FLOAT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- 7. Workflows
CREATE TABLE workflows (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    step_name VARCHAR(150) NOT NULL,
    required_role VARCHAR(100) NOT NULL,
    current_status VARCHAR(50) DEFAULT 'Pending',
    comment TEXT,
    validated_by UUID REFERENCES users(id),
    validated_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- 8. Audit Logs (Logique immuable : pas d'updated_at)
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    changes TEXT,
    user_id VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- --- SEEDING ---
INSERT INTO roles (name, code) VALUES 
('Bureau d''Ordre', 'ROLE_BO'), ('Finance', 'ROLE_FINANCE'), ('Support Technique', 'ROLE_TECH');

INSERT INTO statuses (name, code) VALUES 
('En attente d''indexation', 'TECH_TO_INDEX'),    
('En attente de validation', 'BUS_PENDING_VAL'), 
('Validé pour paiement', 'BUS_VALIDATED'),       
('Rejeté', 'BUS_REJECTED');