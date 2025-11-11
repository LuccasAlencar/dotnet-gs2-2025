// Configuração da API
const API_BASE_URL = 'http://localhost:5000/api/v1';

// Elementos do DOM
const curriculoInput = document.getElementById('curriculoInput');
const fileName = document.getElementById('fileName');
const curriculoInfo = document.getElementById('curriculoInfo');
const keywordsDiv = document.getElementById('keywords');
const searchForm = document.getElementById('searchForm');
const loading = document.getElementById('loading');
const resultsSection = document.getElementById('resultsSection');
const resultsCount = document.getElementById('resultsCount');
const jobsList = document.getElementById('jobsList');

// Palavras-chave extraídas do currículo
let extractedKeywords = [];

// Upload de Currículo
curriculoInput.addEventListener('change', async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    fileName.textContent = file.name;
    
    // Simular extração de palavras-chave
    // Em produção, aqui você enviaria o arquivo para uma API de processamento/IA
    await extractKeywordsFromCurriculo(file);
});

// Função simulada de extração de palavras-chave
async function extractKeywordsFromCurriculo(file) {
    // Simulação de processamento
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // Palavras-chave simuladas (em produção viriam de IA/NLP)
    const simulatedKeywords = [
        'javascript', 'react', 'nodejs', 'sql', 'git',
        'agile', 'scrum', 'api', 'frontend', 'backend'
    ];
    
    extractedKeywords = simulatedKeywords;
    
    // Exibir palavras-chave
    keywordsDiv.innerHTML = '';
    extractedKeywords.forEach(keyword => {
        const span = document.createElement('span');
        span.className = 'keyword';
        span.textContent = keyword;
        keywordsDiv.appendChild(span);
    });
    
    curriculoInfo.classList.remove('hidden');
    
    // Preencher campo de busca com primeira palavra-chave
    if (extractedKeywords.length > 0) {
        document.getElementById('cargo').value = extractedKeywords[0];
    }
}

// Buscar Vagas
searchForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const cargo = document.getElementById('cargo').value.trim();
    const localizacao = document.getElementById('localizacao').value.trim();
    const categoria = document.getElementById('categoria').value;
    
    if (!cargo) {
        alert('Por favor, informe um cargo ou palavra-chave');
        return;
    }
    
    await buscarVagas(cargo, localizacao, categoria);
});

// Função para buscar vagas na API
async function buscarVagas(cargo, localizacao, categoria) {
    try {
        // Mostrar loading
        loading.classList.remove('hidden');
        resultsSection.classList.add('hidden');
        
        // Fazer requisição para a API
        const response = await fetch(`${API_BASE_URL}/jobs/search`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                cargo: cargo,
                localizacao: localizacao || 'brasil',
                categoria: categoria || 'it-jobs',
                pagina: 1,
                resultadosPorPagina: 20
            })
        });
        
        if (!response.ok) {
            throw new Error('Erro ao buscar vagas');
        }
        
        const data = await response.json();
        
        // Ocultar loading
        loading.classList.add('hidden');
        
        // Exibir resultados
        exibirResultados(data);
        
    } catch (error) {
        console.error('Erro:', error);
        loading.classList.add('hidden');
        
        // Exibir erro
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error';
        errorDiv.textContent = 'Erro ao buscar vagas. Verifique se a API está rodando e se as credenciais do Adzuna estão configuradas.';
        document.querySelector('.container').insertBefore(errorDiv, resultsSection);
        
        setTimeout(() => errorDiv.remove(), 5000);
    }
}

// Função para exibir resultados
function exibirResultados(data) {
    const jobs = data.results || [];
    
    if (jobs.length === 0) {
        resultsCount.textContent = 'Nenhuma vaga encontrada';
        jobsList.innerHTML = '<p style="text-align: center; color: #666;">Tente ajustar os filtros de busca.</p>';
    } else {
        resultsCount.textContent = `${jobs.length} vaga(s) encontrada(s)`;
        
        jobsList.innerHTML = '';
        
        jobs.forEach(job => {
            const jobCard = criarJobCard(job);
            jobsList.appendChild(jobCard);
        });
    }
    
    resultsSection.classList.remove('hidden');
    
    // Scroll suave até os resultados
    resultsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Função para criar card de vaga
function criarJobCard(job) {
    const card = document.createElement('div');
    card.className = 'job-card';
    
    // Debug: ver estrutura do job
    console.log('Job data:', job);
    
    const title = document.createElement('h3');
    title.className = 'job-title';
    title.textContent = job.title || 'Título não disponível';
    
    const company = document.createElement('p');
    company.className = 'job-company';
    const companyName = job.company?.display_name || 'Empresa não informada';
    company.textContent = `🏢 ${companyName}`;
    
    const location = document.createElement('p');
    location.className = 'job-location';
    const locationName = job.location?.display_name || 'Localização não informada';
    location.textContent = `📍 ${locationName}`;
    
    const salary = document.createElement('p');
    salary.className = 'job-salary';
    const salaryMin = job.salary_min;
    const salaryMax = job.salary_max;
    
    if (salaryMin && salaryMax) {
        salary.textContent = `💰 R$ ${Math.round(salaryMin).toLocaleString('pt-BR')} - R$ ${Math.round(salaryMax).toLocaleString('pt-BR')}`;
    } else if (salaryMin) {
        salary.textContent = `💰 A partir de R$ ${Math.round(salaryMin).toLocaleString('pt-BR')}`;
    } else {
        salary.textContent = '💰 Salário não informado';
    }
    
    const link = document.createElement('a');
    link.className = 'job-link';
    const jobUrl = job.redirect_url;
    
    if (jobUrl && jobUrl !== '') {
        link.href = jobUrl;
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        link.textContent = 'Ver vaga';
    } else {
        link.style.background = '#ccc';
        link.style.cursor = 'not-allowed';
        link.textContent = 'Link não disponível';
        link.onclick = (e) => e.preventDefault();
    }
    
    card.appendChild(title);
    card.appendChild(company);
    card.appendChild(location);
    card.appendChild(salary);
    card.appendChild(link);
    
    return card;
}

// Mensagem de boas-vindas
console.log('🚀 Frontend carregado com sucesso!');
console.log('📡 API: ' + API_BASE_URL);
