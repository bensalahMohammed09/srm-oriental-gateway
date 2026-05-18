pipeline {
    agent any

    stages {
        stage('0. Preparation') {
            steps {
                echo "Cleaning workspace and fixing Git permissions..."
                deleteDir()
                sh "git config --global --add safe.directory '*'"
                checkout scm  

                script {
                    config = load 'infra/jenkins/pipeline.conf'
                    echo "Config Loaded for Registry: ${config.DOCKER_NAMESPACE}"
                }                  
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
                    
                    sh 'timeout 120s bash -c "until curl -s http://sonarqube:9002/api/system/status | grep -q UP; do sleep 5; done"'

                    echo "Running SonarQube analysis for Backend..."
                    withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_TOKEN')]) {
                        withSonarQubeEnv('sonarqube') {
                            sh """
                                dotnet sonarscanner begin /k:"srm-backend" \
                                /d:sonar.login="${SONAR_TOKEN}" \
                                /d:sonar.host.url=http://sonarqube:9002 \
                                /d:sonar.exclusions="**/frontend/**,**/node_modules/**,**/*.js,**/*.ts,**/*.tsx,**/*.jsx,**/*.html,**/*.css,**/*Dockerfile,**/*.Dockerfile,**/*.yml,**/*.yaml"

                                dotnet build src/backend/Srm.Gateway.sln --configuration Release
                                dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"
                            """
                        }
                    }

                    timeout(time: 5, unit: 'MINUTES') {
                        def qg = waitForQualityGate()
                        if (qg.status != 'OK') {
                            error 'SonarQube Quality Gate failed: ${qg.status}'
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

        stage('5. Custom Containers Build & Push') {
            steps {
                script {
                    echo "GIT_SHA : ${env.DEPLOY_TAG}"
                    
                    withCredentials([usernamePassword(credentialsId: config.DOCKER_CREDS_ID, usernameVariable: 'DUSER', passwordVariable: 'DPASS')]) {
                        sh 'echo \$DPASS | docker login -u \$DUSER --password-stdin'

                        env.ALL_SERVICES.split(',').each { serviceName ->
                            def varBase = serviceName.toUpperCase().replace('-', '_')

                            if(env["BUILD_${varBase}"] == "true"){
                                echo "BUILDING: ${serviceName} (SHA: ${env.DEPLOY_TAG})"
                                
                                def dockerfileName = serviceName.replace('srm-', '') + ".Dockerfile"
                                def infraPath = "infra/docker/${dockerfileName}"
                                def registryImage = "${config.DOCKER_NAMESPACE}/${serviceName}:${env.DEPLOY_TAG}"

                                // 💡 THE FIX: Dynamically map the build context based on the service name
                                def buildContext = "."
                                if (serviceName == "srm-api") {
                                    buildContext = "src/backend/"
                                } else if (serviceName == "srm-ocr-worker") {
                                    buildContext = "src/workers/ocr-worker/"
                                } else if (serviceName == "srm-dashboard") {
                                    buildContext = "src/frontend/srm-dashboard/"
                                }

                                // 💡 THE FIX: Remove awk/env and inject build-args safely. 
                                // Docker automatically ignores args that aren't used in the specific Dockerfile!
                                sh """
                                    docker build \\
                                        --build-arg DOTNET_VERSION=9.0 \\
                                        --build-arg PYTHON_VERSION=3.11 \\
                                        --build-arg NODE_VERSION=20 \\
                                        -t ${serviceName}:latest \\
                                        -t ${registryImage} \\
                                        -f ${infraPath} ${buildContext}
                                """

                                if(env["PUSH_${varBase}"] == "true"){
                                    echo "PUSHING: ${registryImage} to Docker Hub..."
                                    sh "docker push ${registryImage}"
                                }
                            } else {
                                echo "SKIP: No changes for ${serviceName}"
                            }
                        }
                    }
                } 
            }
        }

       stage('6. Prepare CI Volumes & Local Integration') {
            steps {
                script {
                    echo "Nettoyage et injection des configurations dans les volumes Docker..."
                    def projectPrefix = "srm-oriental-gateway"
                        
                    sh """
                        docker volume rm ${projectPrefix}_loki_config || true
                        docker volume rm ${projectPrefix}_prometheus_config || true
                        docker volume rm ${projectPrefix}_promtail_config || true
                        docker volume rm ${projectPrefix}_admin_conf || true
                        docker volume rm ${projectPrefix}_public_conf || true
                        docker volume rm ${projectPrefix}_grafana_provisioning || true
                        
                        docker volume create ${projectPrefix}_loki_config
                        docker volume create ${projectPrefix}_prometheus_config
                        docker volume create ${projectPrefix}_promtail_config
                        docker volume create ${projectPrefix}_admin_conf
                        docker volume create ${projectPrefix}_public_conf
                        docker volume create ${projectPrefix}_grafana_provisioning
                        
                        docker volume create ${projectPrefix}_ocr_uploads || true
                        docker run --rm -i -v ${projectPrefix}_ocr_uploads:/dest alpine sh -c 'mkdir -p /dest/pending /dest/processed /dest/failed /dest/archived && chmod -R 777 /dest'
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

                    echo "Démarrage de la stack locale avec Docker Compose..."
                    withCredentials([file(credentialsId: 'srm-env-file', variable: 'SECRET_ENV')]) {
                        sh '''
                            cat "$SECRET_ENV" | tr -d '\r' > clean.env
                            sed -e 's|.*- \\.\\/infra\\/.*|      - /dev/null:/tmp/dummy|g' -e 's|.*\\/app\\/uploads.*|      - /dev/null:/tmp/dummy|g' docker-compose.yml > docker-compose.clean.yml

                            SERVICES=$(docker compose -f docker-compose.clean.yml -f docker-compose.ci.yml --env-file clean.env config --services | grep -vE 'jenkins-srm|sonarqube|sonar-db')

                            docker compose -f docker-compose.clean.yml -f docker-compose.ci.yml --env-file clean.env up -d \
                                --force-recreate \
                                --always-recreate-deps \
                                $SERVICES

                            echo "Waiting 20s for local stack stabilization..."
                            sleep 20

                            if [ "$BUILD_SRM_API" = "true" ]; then
                                echo "Probing Local API..."
                                # FIX: Changed from curl on port 5000 to wget on port 9000
                                docker exec srm-api wget --no-verbose --tries=1 --spider http://localhost:9000/health || exit 1
                            fi

                            if [ "$BUILD_SRM_OCR_WORKER" = "true" ]; then
                                echo "Verifying Local OCR Worker..."
                                if ! docker ps --filter "name=srm-ocr-worker" --filter "status=running" | grep -q srm-ocr-worker; then
                                    echo "OCR Worker crashed locally!"
                                    exit 1
                                fi
                            fi

                            rm clean.env docker-compose.clean.yml
                        '''
                    }
                }
            }
            // THE POST BLOCK HAS BEEN COMPLETELY REMOVED. 
            // JENKINS WILL NO LONGER KILL YOUR CONTAINERS!
        }

        stage('7. Ansible Surgical Deploy') {
            when {
                expression { env.STABLE_PROMOTION_REQUIRED == "true" }
            }
            steps {
                script {
                    echo "Executing Remote Ansible Deployment..."
                    
                    def targets = []
                    env.ALL_SERVICES.split(',').each { svc ->
                        if (env["PUSH_${svc.toUpperCase().replace('-', '_')}"] == "true") { 
                            targets.add(svc) 
                        }
                    }

                    if (targets.isEmpty()) {
                        echo "NO PUSH DETECTED: Skipping Ansible deployment."
                        return
                    }

                    env.TARGET_SERVICES_LIST = targets.join(' ')
                    echo "TARGETS FOR ANSIBLE: ${env.TARGET_SERVICES_LIST}"
                    env.ANSIBLE_RAN = "true"

                    // ENTERING ANSIBLE FOLDER
                    dir('infra/ansible') {
                        withCredentials([sshUserPrivateKey(credentialsId: 'srm-server-ssh', keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')]) {
                            sh """
                                export ANSIBLE_HOST_KEY_CHECKING=False
                                ansible-playbook -i inventory/production.ini deploy.yml \\
                                    -u \$SSH_USER --private-key "\$SSH_KEY" \\
                                    -e "image_tag=${env.DEPLOY_TAG}" \\
                                    -e "target_services='${env.TARGET_SERVICES_LIST}'"
                            """
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            deleteDir()
            echo "Workspace cleaned."
        }
        success {
            script {
                echo "SUCCESS: Pipeline finished."
                if (env.ANSIBLE_RAN == "true" && env.TARGET_SERVICES_LIST) {
                    echo "✅ ANSIBLE VERIFIED: Promoting to :stable in Docker Hub..."
                    withCredentials([usernamePassword(credentialsId: config.DOCKER_CREDS_ID, usernameVariable: 'DUSER', passwordVariable: 'DPASS')]) {
                        sh "echo \$DPASS | docker login -u \$DUSER --password-stdin"
                        env.TARGET_SERVICES_LIST.split(' ').each { service ->
                            sh """
                                docker tag ${config.DOCKER_NAMESPACE}/${service}:${env.DEPLOY_TAG} ${config.DOCKER_NAMESPACE}/${service}:stable
                                docker push ${config.DOCKER_NAMESPACE}/${service}:stable
                            """
                        }
                    }
                }
            }
        }
        failure {
            script {
                echo "FAILED: Pipeline failed. Check the logs!"
                if (env.ANSIBLE_RAN == "true" && env.TARGET_SERVICES_LIST) {
                    echo "🚨 CRITICAL FAILURE: Triggering Ansible Emergency Rollback..."
                    // ENTERING ANSIBLE FOLDER FOR ROLLBACK
                    dir('infra/ansible') {
                        withCredentials([sshUserPrivateKey(credentialsId: 'srm-server-ssh', keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')]) {
                            sh """
                                export ANSIBLE_HOST_KEY_CHECKING=False
                                ansible-playbook -i inventory/production.ini rollback.yml \\
                                    -u \$SSH_USER --private-key "\$SSH_KEY" \\
                                    -e "target_services='${env.TARGET_SERVICES_LIST}'"
                            """
                        }
                    }
                }
            }
        }
    }
}