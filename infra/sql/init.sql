-- Enable UUID extension for Guid support
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Roles
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 2. Users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role_id UUID REFERENCES roles(id) ON DELETE SET NULL,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 3. Categories
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 4. Statuses
CREATE TABLE statuses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 5. Documents
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    reference VARCHAR(100) NOT NULL UNIQUE,
    supplier_name VARCHAR(150),
    total_amount DECIMAL(18,2),
    status_id UUID NOT NULL REFERENCES statuses(id),
    category_id UUID REFERENCES categories(id),
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 6. OCR Metadata (Flexibilité par Key-Value)
CREATE TABLE ocr_metadata (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    key VARCHAR(100) NOT NULL,   -- ex: 'TVA', 'Num_Compte'
    value TEXT NOT NULL,         -- La valeur extraite
    confidence FLOAT DEFAULT 0,  -- Score de confiance (0-1)
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 7. Workflows (Étapes de validation dynamiques)
CREATE TABLE workflows (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    step_name VARCHAR(150) NOT NULL,    -- ex: 'Approbation Finance'
    required_role VARCHAR(100) NOT NULL, -- Le code du rôle nécessaire
    current_status VARCHAR(50) DEFAULT 'Pending', -- Pending, Approved, Rejected
    comment TEXT,
    validated_by UUID REFERENCES users(id),
    validated_at TIMESTAMP,
    created_at TIMESTAMP ,
    updated_at TIMESTAMP
);

-- 8. Audit Logs
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL, -- Create, Update, Delete
    changes TEXT,                -- Format JSON des modifications
    user_id VARCHAR(100),
    created_at TIMESTAMP 
);

-- Initial Data Seed
INSERT INTO roles (name, code) VALUES 
('Bureau d''Ordre', 'ROLE_BO'),
('Finance', 'ROLE_FINANCE'),
('Support Technique', 'ROLE_TECH');

INSERT INTO statuses (name, code) VALUES 
('En attente d''indexation', 'PENDING_INDEX'),
('En attente de validation', 'PENDING_VAL'),
('Validé pour paiement', 'VALIDATED'),
('Rejeté', 'REJECTED');

INSERT INTO categories (name, description) VALUES 
('Facture Télécom', 'Factures Orange, IAM, Inwi'),
('Maintenance', 'Contrats de maintenance technique'),
('Achat Fourniture', 'Bureautique et consommables');