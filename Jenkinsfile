pipeline {
    agent any

      stages {
            stage('0. Preparation') {
                steps {
                    echo "Cleaning workspace and fixing Git permissions..."
                    deleteDir()

                    sh "git config --global --add safe.directory '*'"
                    checkout scm                    
                }
            }

            stage('1. Intelligent Path Detection'){
                steps{
                    script{
                        echo "Running Smart Build Python Script..."
                        sh "python3 infra/jenkins/scripts/detect_changes.py"

                        echo "Loading variables into Jenkins environment...."
                        def props = readProperties file:'build_triggers.properties'
                        props.each { key, value ->
                            env[key] = value
                        }

                        echo "------- LOADED ENV VARIABLES -------"
                        sh "cat build_triggers.properties"
                    }
                }
            }

            stage('2. Backend Analysis with SonarQube') {
                when { 
                    expression { env.RUN_SONAR_BACKEND == "true" } 
                }
                steps {
                    script {
                        try {
                            sh "docker start sonar-db sonarqube"
                            
                            // FIX: Added 'sh' wrapper here
                            //sh 'curl -X POST "http://sonarqube:9002/api/system/migrate_db"'
                            
                            sh 'timeout 120s bash -c "until curl -s http://sonarqube:9002/api/system/status | grep -q UP; do sleep 5; done"'

                            echo "Running SonarQube analysis for Backend..."
                            withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_TOKEN')]) {
                                withSonarQubeEnv('sonarqube') {
                                    sh """
                                        dotnet sonarscanner begin /k:"srm-backend" \
                                        /d:sonar.login="${SONAR_TOKEN}" \
                                        /d:sonar.host.url=http://sonarqube:9002

                                        dotnet build src/backend/Srm.Gateway.sln --configuration Release
                                        dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"
                                    """
                                }
                            }

                            timeout(time: 5, unit: 'MINUTES') {
                                def qg = waitForQualityGate()
                                if (qg.status != 'OK') {
                                    error "SonarQube Quality Gate failed: ${qg.status}"
                                }
                            }
                        } finally {
                            sh "docker stop sonar-db sonarqube || true"
                        }
                    } // Closes script
                } // Closes steps
            } // Closes stage

            stage('3. Frontend & Worker Scan') {
                parallel {
                    stage('Frontend - NPM Audit') {
                        when {
                            expression { env.RUN_SCAN_FRONTEND == "true"}
                        }
                        steps {
                            dir('src/frontend') {
                                echo "Running NPM Audit for Frontend..."
                                sh "npm audit --audit-level=high"
                            }
                        }
                    }
                    stage('Worker - Bandit Scan') {
                        when {
                            expression { env.RUN_SCAN_WORKER == "true"}
                        }
                        steps {
                            dir('src/workers/ocr-service') {
                                echo "Running Bandit Scan for Worker..."
                                sh "bandit -r . -ll -ii"
                            }
                        }
                    }
                }
            }

            stage('4. Mandatory Full Scan') {
                when {
                    expression { env.RUN_TRIVY == "true" }
                }
                steps {
                    echo "Running full scans for all components due to critical changes..."
                    sh "trivy fs --severity HIGH,CRITICAL --exit-code 1 ."
                }
            }

            stage('5. Custom Containers build') {
                steps {
                script {

                        
                        def commitSha = readFile('.git_sha').trim()
                        echo "GIT_SHA : ${commitSha}"
                        def services = [
                            'infra/docker/api.Dockerfile' : 'srm-api',
                            'infra/docker/db.Dockerfile' : 'srm-db',
                            'infra/docker/dashboard.Dockerfile' : 'srm-dashboard',
                            'infra/docker/worker.Dockerfile' : 'srm-ocr-worker',
                            'infra/docker/promtail.Dockerfile' : 'promtail',
                            'infra/docker/node-exporter.Dockerfile' : 'node-exporter',
                            'infra/docker/sonar-db.Dockerfile' : 'sonar-db',
                            'infra/docker/sonarqube.Dockerfile' : 'sonarqube',
                            'infra/docker/grafana.Dockerfile' : 'grafana',
                        ] 

                        services.each {infraPath, imageName ->
                            def varName = "BUILD_${imageName.toUpperCase().replace('-','_')}"

                            if(env[varName] == "true"){
                                echo "BUILDING: ${imageName} (SHA: ${commitSha})"
                            sh """
                                    # 1. Load and Export local environment variables
                                    set -a && source .env && set +a
                                    
                                    # 2. Normalize variables for Cross-Platform compatibility 
                                    # This strips Windows Carriage Returns (\r) to ensure Linux/Docker compatibility
                                    BUILD_ARGS=\$(grep -v '^#' .env | tr -d '\r' | xargs -I {} echo "--build-arg {}" | xargs)

                                    # 3. Execute build with dynamically injected arguments
                                    docker build \$BUILD_ARGS \
                                        -t ${imageName}:${commitSha} \
                                        -f ${infraPath} .
                                """
                            } else{
                                echo "SKIP: No changes for ${imageName}"
                            }
                        }
                    } 
                }
            }
        

            stage('6. Prepare CI Configuration Volumes') {
                steps {
                    script {
                        echo "Nettoyage et injection des configurations dans les volumes Docker..."
                        def projectPrefix = "srm-oriental-gateway"
                            
                            sh """
                                # 1. Suppression des anciens volumes (On ne supprime pas ocr_uploads pour garder les fichiers !)
                                docker volume rm ${projectPrefix}_loki_config || true
                                docker volume rm ${projectPrefix}_prometheus_config || true
                                docker volume rm ${projectPrefix}_promtail_config || true
                                docker volume rm ${projectPrefix}_admin_conf || true
                                docker volume rm ${projectPrefix}_public_conf || true
                                docker volume rm ${projectPrefix}_grafana_provisioning || true
                                
                                # 2. Création des volumes de config
                                docker volume create ${projectPrefix}_loki_config
                                docker volume create ${projectPrefix}_prometheus_config
                                docker volume create ${projectPrefix}_promtail_config
                                docker volume create ${projectPrefix}_admin_conf
                                docker volume create ${projectPrefix}_public_conf
                                docker volume create ${projectPrefix}_grafana_provisioning
                                
                                # 3. FIX PERMISSIONS : Création du volume OCR s'il n'existe pas
                                docker volume create ${projectPrefix}_ocr_uploads || true
                                docker run --rm -i -v ${projectPrefix}_ocr_uploads:/dest alpine chmod 777 /dest
                            """
                            
                            echo "Injection des fichiers de configuration..."
                            sh "cat infra/loki/loki-config.yml | docker run --rm -i -v ${projectPrefix}_loki_config:/dest alpine sh -c 'cat > /dest/local-config.yaml'"
                            sh "cat infra/prometheus/prometheus.yml | docker run --rm -i -v ${projectPrefix}_prometheus_config:/dest alpine sh -c 'cat > /dest/prometheus.yml'"
                            sh "cat infra/promtail/promtail-config.yml | docker run --rm -i -v ${projectPrefix}_promtail_config:/dest alpine sh -c 'cat > /dest/config.yml'"
                            sh "cat infra/nginx/admin.conf | docker run --rm -i -v ${projectPrefix}_admin_conf:/dest alpine sh -c 'cat > /dest/default.conf'"
                            sh "cat infra/nginx/public.conf | docker run --rm -i -v ${projectPrefix}_public_conf:/dest alpine sh -c 'cat > /dest/default.conf'"
                            sh "cat infra/nginx/security_headers.conf | docker run --rm -i -v ${projectPrefix}_public_conf:/dest alpine sh -c 'cat > /dest/security_headers.conf'"
                            
                            echo "Injection du dossier de provisioning Grafana..."
                            sh "tar -cC infra/grafana/provisioning . | docker run --rm -i -v ${projectPrefix}_grafana_provisioning:/dest alpine tar -x -C /dest"
                        }
                    }
            }


           stage('7. Deploy SRM Stack') {
                steps {
                    script {
                        echo "Démarrage de la stack avec Docker Compose..."
                        withCredentials([file(credentialsId: 'srm-env-file', variable: 'SECRET_ENV')]) {
                            sh '''
                                # 1. Nettoyage des caractères Windows
                                cat "$SECRET_ENV" | tr -d '\r' > clean.env

                                # 2. Le Chirurgien V2 : Remplace les lignes par un montage fantôme pour garder le YAML valide
                                sed -e 's|.*- \\.\\/infra\\/.*|      - /dev/null:/tmp/dummy|g' -e 's|.*\\/app\\/uploads.*|      - /dev/null:/tmp/dummy|g' docker-compose.yml > docker-compose.clean.yml

                                # 3. Récupération des services cibles (EXCLUT Jenkins et Sonar)
                                SERVICES=$(docker compose -f docker-compose.clean.yml -f docker-compose.ci.yml --env-file clean.env config --services | grep -vE 'jenkins-srm|sonarqube|sonar-db')

                                # 4. Déploiement
                                docker compose -f docker-compose.clean.yml -f docker-compose.ci.yml --env-file clean.env up -d \
                                    --force-recreate \
                                    --always-recreate-deps \
                                    --remove-orphans \
                                    $SERVICES

                                # 5. Nettoyage
                                rm clean.env docker-compose.clean.yml
                            '''
                        }
                    }
                }
           }
        }
    

    post{
        always{
            deleteDir()
            echo "clean up the workspace"
        }
        success{
            echo "SUCCESS: Pipeline finished."
        }
        failure{
            echo "FAILED: Pipeline failed. Check the logs!"
        }
    }
}