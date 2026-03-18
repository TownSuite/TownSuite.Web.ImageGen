
library 'ts-jenkins-shared-library@main'

pipeline {
    agent none
    options {
        copyArtifactPermission('*/TownSuite-Artifact-Publish')
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timestamps()
        timeout(time: 2, unit: 'HOURS')
    }
    stages {
        stage('Start Automation Script') {
            agent { label 'starting-agent' }
            steps {
                script {
                    townsuite_automation2.start_linux()
                }
            }
        }
        stage('Pipeline') {
            agent { label townsuite_automation2.get_ubuntu_label() }
            stages{
                stage('Linux Build') {
                    steps {
                        script {
                            townsuite.common_environment_configuration()
                        }

                        sh '''
                        chmod +x ./build.ps1
                        ./build.ps1
                        '''
                    
                        echo 'archiving artifacts'
                        script {
                            townsuite.archiveWithRetryAndLock('build/**/*.tar,build/parameterproperties.txt', 3)
                        }
                    }
                }
            }
        }
    }
    post {
        always {
            CleanupVirtualMachines()
        }
        success {
            echo 'Pipeline executed successfully.'
        }
        failure {
            echo 'Pipeline failed.'
        }
        aborted {
            echo 'Pipeline was aborted.'
        }
    }
}

def CleanupVirtualMachines() {
    node('stopping-agent') {
        cleanWs()
        script {
            townsuite_automation2.stop_automation()
        }
    }
}
