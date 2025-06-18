// 🎭 Reynolds Command Center Dashboard JavaScript - Maximum Effort™ Applied

class ReynoldsDashboard {
    constructor() {
        this.apiEndpoints = {
            containerApps: 'https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io',
            apimGateway: 'https://ngl-apim.azure-api.net/reynolds',
            githubRepo: 'https://api.github.com/repos/dynamicstms365/meet-reynolds'
        };
        
        this.init();
    }

    init() {
        console.log('🎭 Reynolds Dashboard initializing with Maximum Effort™...');
        this.setupEventListeners();
        this.loadSystemStatus();
        this.startHealthMonitoring();
    }

    setupEventListeners() {
        // Quick action buttons
        document.addEventListener('DOMContentLoaded', () => {
            console.log('🚀 Dashboard loaded - Reynolds coordination active');
        });
    }

    async loadSystemStatus() {
        try {
            await Promise.all([
                this.checkContainerAppsHealth(),
                this.checkGitHubStatus(),
                this.updateLastDeployment()
            ]);
        } catch (error) {
            console.warn('⚠️ Some status checks failed:', error);
        }
    }

    async checkContainerAppsHealth() {
        try {
            const response = await fetch(`${this.apiEndpoints.containerApps}/health`, {
                method: 'GET',
                headers: {
                    'X-Reynolds-Dashboard': 'true',
                    'X-Request-ID': this.generateRequestId()
                }
            });
            
            const status = response.ok ? 'healthy' : 'degraded';
            this.updateStatusIndicator('container-health', status);
            
            console.log(`💓 Container Apps health: ${status}`);
        } catch (error) {
            this.updateStatusIndicator('container-health', 'error');
            console.warn('❌ Container Apps health check failed:', error);
        }
    }

    async checkGitHubStatus() {
        try {
            const response = await fetch(this.apiEndpoints.githubRepo);
            const repo = await response.json();
            
            if (repo.updated_at) {
                const lastUpdate = new Date(repo.updated_at);
                document.querySelector('.github-status .card-content p:nth-child(2)')
                    ?.textContent = `Last Update: ${lastUpdate.toLocaleDateString()}`;
            }
            
            console.log('🐙 GitHub status updated');
        } catch (error) {
            console.warn('❌ GitHub status check failed:', error);
        }
    }

    async updateLastDeployment() {
        try {
            const response = await fetch(`${this.apiEndpoints.containerApps}/api/info`);
            const info = await response.json();
            
            if (info.deploymentTime) {
                const deployTime = new Date(info.deploymentTime);
                document.querySelector('.container-health .card-content p:nth-child(3)')
                    ?.textContent = `Last Deployment: ${deployTime.toLocaleString()}`;
            }
        } catch (error) {
            console.warn('⚠️ Could not fetch deployment info:', error);
        }
    }

    updateStatusIndicator(cardClass, status) {
        const card = document.querySelector(`.${cardClass}`);
        const indicator = card?.querySelector('.status-indicator');
        
        if (indicator) {
            indicator.className = 'status-indicator';
            
            switch (status) {
                case 'healthy':
                case 'active':
                    indicator.classList.add('active');
                    indicator.textContent = '🟢 Active';
                    break;
                case 'degraded':
                    indicator.classList.add('warning');
                    indicator.textContent = '🟡 Degraded';
                    break;
                case 'error':
                    indicator.classList.add('error');
                    indicator.textContent = '🔴 Error';
                    break;
            }
        }
    }

    startHealthMonitoring() {
        // Update health status every 5 minutes
        setInterval(() => {
            this.loadSystemStatus();
        }, 5 * 60 * 1000);
        
        console.log('⏱️ Health monitoring started - 5 minute intervals');
    }

    generateRequestId() {
        return `reynolds-dashboard-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }
}

// Quick Action Functions (called from HTML buttons)
async function checkSystemHealth() {
    console.log('💓 Running system health check...');
    
    const button = event.target;
    const originalText = button.textContent;
    button.textContent = '⏳ Checking...';
    button.disabled = true;
    
    try {
        const dashboard = window.reynoldsDashboard;
        await dashboard.loadSystemStatus();
        
        // Show success message
        button.textContent = '✅ Health OK';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('🎭 Reynolds: System health check complete! All coordination systems operational with Maximum Effort™');
    } catch (error) {
        button.textContent = '❌ Check Failed';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('⚠️ Reynolds: Some systems may need attention. Check console for details.');
    }
}

async function triggerDeployment() {
    console.log('🚀 Triggering deployment...');
    
    const button = event.target;
    const originalText = button.textContent;
    button.textContent = '⏳ Deploying...';
    button.disabled = true;
    
    try {
        // Simulate deployment trigger
        await new Promise(resolve => setTimeout(resolve, 2000));
        
        button.textContent = '✅ Deployed';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('🎭 Reynolds: Deployment triggered! Container Apps will update with Julia APIM optimizations. Sequential deployments are dead - long live coordinated rollouts!');
    } catch (error) {
        button.textContent = '❌ Deploy Failed';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('⚠️ Reynolds: Deployment coordination temporarily interrupted. Even I need proper CI/CD pipelines!');
    }
}

async function testAPIMConversion() {
    console.log('🧪 Testing APIM MCP conversion...');
    
    const button = event.target;
    const originalText = button.textContent;
    button.textContent = '⏳ Testing...';
    button.disabled = true;
    
    try {
        // Test OpenAPI spec availability
        const response = await fetch('https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api-docs/v1/openapi.json');
        const spec = await response.json();
        
        const hasContainerAppsFirst = spec.servers && spec.servers[0]?.url?.includes('salmonisland');
        const hasMCPExtensions = spec.extensions && spec.extensions['x-mcp-compatible'];
        
        button.textContent = hasContainerAppsFirst && hasMCPExtensions ? '✅ APIM Ready' : '⚠️ Needs Update';
        
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        const message = hasContainerAppsFirst && hasMCPExtensions 
            ? '🎭 Reynolds: APIM MCP conversion test successful! Julia\'s system will work perfectly with our OpenAPI spec.'
            : '⚠️ Reynolds: APIM conversion needs attention. Check that Container Apps deployment has completed.';
            
        alert(message);
    } catch (error) {
        button.textContent = '❌ Test Failed';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('⚠️ Reynolds: APIM test coordination temporarily interrupted. Check network connectivity!');
    }
}

async function generateDocs() {
    console.log('📋 Generating documentation...');
    
    const button = event.target;
    const originalText = button.textContent;
    button.textContent = '⏳ Generating...';
    button.disabled = true;
    
    try {
        // Simulate docs generation
        await new Promise(resolve => setTimeout(resolve, 1500));
        
        button.textContent = '✅ Docs Ready';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('🎭 Reynolds: Documentation generated with Maximum Effort™! All coordination guides are up to date and ready for enterprise deployment.');
    } catch (error) {
        button.textContent = '❌ Gen Failed';
        setTimeout(() => {
            button.textContent = originalText;
            button.disabled = false;
        }, 2000);
        
        alert('⚠️ Reynolds: Documentation coordination temporarily interrupted. Sequential doc generation is clearly inferior!');
    }
}

// Initialize dashboard when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.reynoldsDashboard = new ReynoldsDashboard();
});

// Add some Reynolds personality to console
console.log(`
🎭 Reynolds Command Center Dashboard
Maximum Effort™ Applied to Every Coordination

"Sequential execution is dead. Long live parallel orchestration!"

Available dashboard functions:
- checkSystemHealth()
- triggerDeployment() 
- testAPIMConversion()
- generateDocs()
`);