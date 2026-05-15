import subprocess
import sys
import os

def get_git_sha():
    """Retrieves the short SHA of the latest commit for tagging."""
    try:
        result = subprocess.run(
            ['git', 'rev-parse', '--short', 'HEAD'],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError:
        return "latest" # Fallback if git fails

def get_changed_files():
    """Retrieves the list of changed files from the latest Git commit."""
    try:
        result = subprocess.run(
            ['git', 'diff-tree', '--no-commit-id', '--name-only', '-r', 'HEAD'],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )
        # Clean up output handling cross-platform line endings
        return [line.strip() for line in result.stdout.splitlines() if line.strip()]
    except subprocess.CalledProcessError as e:
        print(f"GIT Error: {e.stderr}")
        sys.exit(1)

def get_service_map():
    """
    1. Upgrading the Service Map (The "Brain" Refactor)
    Maps services to their definitions, source code paths, and infrastructure attributes.
    """
    return {
        'srm-api': {
            'dockerfile': 'infra/docker/api.Dockerfile',
            'source_dir': 'src/backend/',
            'type': 'core',
            'can_push': True
        },
        'srm-dashboard': {
            'dockerfile': 'infra/docker/dashboard.Dockerfile',
            'source_dir': 'src/frontend/',
            'type': 'core',
            'can_push': True
        },
        'srm-ocr-worker': {
            'dockerfile': 'infra/docker/worker.Dockerfile',
            'source_dir': 'src/workers/ocr-service/',
            'type': 'core',
            'can_push': True
        },
        'srm-db': {
            'dockerfile': 'infra/docker/db.Dockerfile',
            'source_dir': None,
            'type': 'infra',
            'can_push': False
        },
        'promtail': {
            'dockerfile': 'infra/docker/promtail.Dockerfile',
            'source_dir': None,
            'type': 'infra',
            'can_push': False
        },
        'node-exporter': {
            'dockerfile': 'infra/docker/node-exporter.Dockerfile',
            'source_dir': None,
            'type': 'infra',
            'can_push': False
        },
        'sonar-db': {
            'dockerfile': 'infra/docker/sonar-db.Dockerfile',
            'source_dir': None,
            'type': 'infra',
            'can_push': False
        },
        'sonarqube': {
            'dockerfile': 'infra/docker/sonarqube.Dockerfile',
            'source_dir': None,
            'type': 'infra',
            'can_push': False
        },
        'grafana': {
            'dockerfile': 'infra/docker/grafana.Dockerfile',
            'source_dir': None,
            'type': 'infra',
            'can_push': False
        }
    }

def main():
    print("---- Starting Smart Build Pipeline (Python) ----")
    changed_files = get_changed_files()

    print(f"Detected {len(changed_files)} changed file(s):")
    for f in changed_files:
        print(f"-> {f}")

    # 5. Intelligence for "Facture" Traceability
    # Validating environment integrity
    pipeline_error = not os.path.exists('.env')
    if pipeline_error:
        print("\n[WARNING] .env file is missing! Pipeline may lack necessary credentials.")

    # 3. Standardized Versioning & Rollback Metadata
    deploy_tag = get_git_sha()
    rollback_tag = "stable"
    
    triggers = {
        "DEPLOY_TAG": deploy_tag,
        "ROLLBACK_TAG": rollback_tag,
        "PIPELINE_ERROR": str(pipeline_error).lower()
    }

    # Core Logic Base
    force_all = 'docker-compose.yml' in changed_files or '.env.example' in changed_files
    triggers["FORCE_ALL"] = str(force_all).lower()

    # 2. Global Configuration Watchlist
    observability_dirs = ['infra/loki/', 'infra/prometheus/', 'infra/grafana/dashboards/']
    restart_stack = force_all or any(any(f.startswith(w) for w in observability_dirs) for f in changed_files)
    triggers["RESTART_STACK"] = str(restart_stack).lower()

    # Dynamic scanning state
    stable_promotion_required = False
    run_sonar_backend = False
    run_scan_frontend = False
    run_scan_worker = False
    infra_changed = False

    service_map = get_service_map()

    # Match changes against the Rich Service Map
    for service_name, config in service_map.items():
        var_base = service_name.upper().replace('-', '_')
        
        # Determine if this specific service changed based on its Dockerfile or Source Code
        service_changed = force_all
        if config['dockerfile'] in changed_files:
            service_changed = True
        if config['source_dir'] and any(f.startswith(config['source_dir']) for f in changed_files):
            service_changed = True

        # 4. Expanding the Output (BUILD_ vs PUSH_)
        triggers[f"BUILD_{var_base}"] = str(service_changed).lower()
        triggers[f"PUSH_{var_base}"] = str(service_changed and config['can_push']).lower()

        # Update Pipeline intelligence flags
        if service_changed:
            if config['type'] == 'core':
                stable_promotion_required = True
            elif config['type'] == 'infra':
                infra_changed = True

            # Map to specific scan jobs based on which component changed
            if service_name == 'srm-api':
                run_sonar_backend = True
            elif service_name == 'srm-dashboard':
                run_scan_frontend = True
            elif service_name == 'srm-ocr-worker':
                run_scan_worker = True

    # Finalize intelligent pipeline flags
    triggers["STABLE_PROMOTION_REQUIRED"] = str(stable_promotion_required).lower()
    
    # 1. Base Scanning Triggers Update
    run_trivy = force_all or run_scan_frontend or run_scan_worker or run_sonar_backend or infra_changed
    
    triggers["RUN_SONAR_BACKEND"] = str(run_sonar_backend).lower()
    triggers["RUN_SCAN_FRONTEND"] = str(run_scan_frontend).lower()
    triggers["RUN_SCAN_WORKER"] = str(run_scan_worker).lower()
    triggers["RUN_TRIVY"] = str(run_trivy).lower()

    # 4. Write Output with Cross-Platform Normalization
    with open("build_triggers.properties", "w", newline='\n') as f:
        for key, value in triggers.items():
            # Strip \r just to be extra safe for Linux Jenkins environments
            clean_value = str(value).replace('\r', '') 
            f.write(f"{key}={clean_value}\n")

    # Debug Output
    print("\n=======================================")
    print("----- INJECTED VARIABLES -------")
    for key, value in triggers.items():
        print(f"{key:<25} : {value}")
    print("=======================================")

if __name__ == "__main__":
    main()