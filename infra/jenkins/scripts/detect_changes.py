import subprocess, sys

def get_changed_files():
    """Retrieves the list of changed files from the latest Git commit."""
    try:
        result = subprocess.run(
            ['git','diff-tree', '--no-commit-id','--name-only', '-r','HEAD'],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )

        # Clean up output handling croos-platform line endings
        return [Line.strip() for Line in result.stdout.splitlines() if Line.strip()]
    except subprocess.CalledProcessError as e:
        print(f"GIT Error: {e.stderr}")
        sys.exit(1)

def get_service_map():
    """Maps infrastructure files to their respective image names."""
    return{
        'infra/docker/api.Dockerfile' : 'srm-api',
        'infra/docker/db.Dockerfile' : 'srm-db',
        'infra/docker/dashboard.Dockerfile' : 'srm-dashboard',
        'infra/docker/worker.Dockerfile' : 'srm-ocr-worker',
        'infra/docker/promtail.Dockerfile' : 'promtail',
        'infra/docker/node-exporter.Dockerfile' : 'node-exporter',
        'infra/docker/sonar-db.Dockerfile' : 'sonar-db',
        'infra/docker/sonarqube.Dockerfile' : 'sonarqube',
        'infra/docker/grafana.Dockerfile' : 'grafana',
    }

def main():
    print("---- Starting Smart Build Pipeline (Python) ----")
    changed_files = get_changed_files()

    print(f"Detected {len(changed_files)} changed file(s):")
    for f in changed_files:
        print(f"-> {f}")
    
    # 1. Base Scanning Triggers (Security & Code Quality)
    force_all = 'docker-compose.yml' in changed_files or '.env.example' in changed_files
    run_sonar = any(f.startswith('src/backend/') for f in changed_files)
    run_npm = any(f.startswith('src/frontend/') for f in changed_files)
    run_bandit = any(f.startswith('src/workers/ocr-service') for f in changed_files)
    
    # 2. Infrastructure Tracking
    infra_changed = any(f.startswith('infra/docker/') for f in changed_files)
    run_trivy = force_all or run_npm or run_bandit or run_sonar or infra_changed

    # Initialize the base environment variables 
    triggers = {
        "FORCE_ALL": str(force_all).lower(),
        "RUN_SONAR_BACKEND": str(run_sonar).lower(),
        "RUN_SCAN_FRONTEND": str(run_npm).lower(),
        "RUN_SCAN_WORKER": str(run_bandit).lower(),
        "RUN_TRIVY": str(run_trivy).lower()
    }

    # 3. Dynamic Build Mapping 
    # Read the map and inject BUILD_ variables based on changed Dockerfiles
    service_map = get_service_map()

    # Initialize all builds to false initially 
    for image_name in service_map.values():
        var_name = f"BUILD_{image_name.upper().replace('-','_')}"
        triggers[var_name] = "true" if force_all else "false"
    
    # Override with true if the specific dockerfile changed
    if not force_all:
        for file_path in changed_files:
            if file_path in service_map:
                image_name = service_map[file_path]
                var_name = f"BUILD_{image_name.upper().replace('-','_')}"
                triggers[var_name] = "true"


    # Generate the properties file for Jenkins
    with open("build_triggers.properties","w",newline='\n') as f:
        for key, value in triggers.items():
            f.write(f"{key}={value}\n")
    
    # 5. Debug Output 
    print("\n=======================================")
    print("----- INJECTED VARIABLES -------")
    for key,value in triggers.items():
        print(f"{key:<25} : {value}")
    print("=======================================")

if __name__ == "__main__":
    main()