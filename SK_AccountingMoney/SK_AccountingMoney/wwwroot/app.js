let currentUser = null;

// Инициализация приложения
document.addEventListener('DOMContentLoaded', async () => {
    await checkAuth();
});

// Проверка аутентификации
async function checkAuth() {
    try {
        const response = await fetch('/api/auth/check');
        const data = await response.json();
        
        if (data.authenticated) {
            currentUser = data.user;
            showMainScreen();
            await loadUserData();
        }
    }
    catch (error) {
    console.error('Ошибка проверки аутентификации:', error);       
    }
}



// Загрузка данных пользователя
async function loadUserData() {
    await Promise.all([
        loadBalance(),
        loadTransactions()
    ]);
    
    document.getElementById('userName').textContent = 
        `👤 ${currentUser.userName || 'Пользователь'} (ID: ${currentUser.telegramId})`;
}

// Загрузка баланса
async function loadBalance() {
    try {
        const response = await fetch('/api/balance');
        const data = await response.json();
        
        const balanceElement = document.getElementById('balanceAmount');
        balanceElement.textContent = `${data.balance.toFixed(2)} ₽`;
        
        // Анимация изменения баланса
        balanceElement.classList.add('balance-update');
        setTimeout(() => balanceElement.classList.remove('balance-update'), 500);
    } catch (error) {
        console.error('Ошибка загрузки баланса:', error);
    }
}

// Загрузка истории транзакций
async function loadTransactions() {
    try {
        const response = await fetch('/api/balance/transactions?limit=20');
        const transactions = await response.json();
        
        const listElement = document.getElementById('transactionsList');        
        
        if (transactions.length === 0) {
            listElement.innerHTML = '<p class="empty">История транзакций пуста</p>';
            return;
        }
        
        listElement.innerHTML = transactions.map(t => {
            const isDeposit = t.type === 'deposit';
            const icon = isDeposit ? '➕' : '➖';
            const sign = isDeposit ? '+' : '-';
            const colorClass = isDeposit ? 'deposit' : 'withdraw';
            
            const userInfo = t.userName ? ` (${t.userName})` : '';

            return `
                <div class="transaction-item ${colorClass}">
                    <div class="transaction-info">
                        <span class="transaction-icon">${icon}</span>
                        <div>
                            <p class="transaction-description">${t.description || 'Без описания'}${userInfo}</p>
                            <p class="transaction-date">${formatDate(t.createdAt)}</p>
                        </div>
                    </div>
                    <div class="transaction-amount ${colorClass}">
                        ${sign}${t.amount.toFixed(2)} €
                    </div>
                </div>
            `;
        }).join('');
    } catch (error) {
        console.error('Ошибка загрузки транзакций:', error);
        document.getElementById('transactionsList').innerHTML = 
            '<p class="error">Ошибка загрузки истории</p>';
    }
}

function openDepositModal() {
    const modal = document.getElementById('depositModal');
    modal.classList.add('show');

    modal.onclick = function (event) {
        if (event.target === modal) {
            closeDepositModal();
        }
    };
}

function closeDepositModal() {
    const modal = document.getElementById('depositModal');
    modal.classList.remove('show');
    document.getElementById('depositAmount').value = '';
    document.getElementById('depositDescription').value = '';
}

function openHistoryModal() {
    const modal = document.getElementById('historyModal');
    modal.classList.add('show');

    loadTransactions();

    modal.onclick = function (event) {
        if (event.target === modal) {
            closeHistoryModal();
        }
    };
}

function closeHistoryModal() {
    const modal = document.getElementById('historyModal');
    modal.classList.remove('show');
}

document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        closeDepositModal();
        closeHistoryModal();
    }
});

// Пополнение баланса
async function deposit() {
    const amount = parseFloat(document.getElementById('depositAmount').value);
    const description = document.getElementById('depositDescription').value;
    
    if (!amount || amount <= 0) {
        showNotification('Введите корректную сумму', 'error');
        return;
    }
    
    try {
        const response = await fetch('/api/balance/deposit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                amount: amount,
                description: description || 'Пополнение баланса'
            })
        });
        
        const data = await response.json();
        
        if (response.ok) {
            showNotification('Баланс успешно пополнен!', 'success');
            closeDepositModal();
            document.getElementById('depositAmount').value = '';
            document.getElementById('depositDescription').value = '';            
            await loadUserData();
        } else {
            showNotification(data.error || 'Ошибка пополнения', 'error');
        }
    } catch (error) {
        console.error('Ошибка пополнения:', error);
        showNotification('Ошибка подключения к серверу', 'error');
    }
}

// Снятие средств
async function withdraw() {
    const amount = parseFloat(document.getElementById('withdrawAmount').value);
    const description = document.getElementById('withdrawDescription').value;
    
    if (!amount || amount <= 0) {
        showNotification('Введите корректную сумму', 'error');
        return;
    }
    
    try {
        const response = await fetch('/api/balance/withdraw', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                amount: amount,
                description: description || 'Снятие средств'
            })
        });
        
        const data = await response.json();
        
        if (response.ok) {
            showNotification('Средства успешно сняты!', 'success');
            document.getElementById('withdrawAmount').value = '';
            document.getElementById('withdrawDescription').value = '';
            await loadUserData();
        } else {
            showNotification(data.error || 'Ошибка снятия', 'error');
        }
    } catch (error) {
        console.error('Ошибка снятия:', error);
        showNotification('Ошибка подключения к серверу', 'error');
    }
}

function showMainScreen() {    
    document.getElementById('mainScreen').classList.remove('hidden');
}

function showNotification(message, type = 'info') {
    const notification = document.getElementById('notification');
    notification.textContent = message;
    notification.className = `notification ${type} show`;
    
    setTimeout(() => {
        notification.classList.remove('show');
    }, 3000);
}

function formatDate(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now - date;
    
    if (diff < 60000) return 'только что';
    if (diff < 3600000) return `${Math.floor(diff / 60000)} мин назад`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)} ч назад`;
    
    return date.toLocaleDateString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// ============================================
// ТЕСТОВЫЕ ФУНКЦИИ (только для разработки)
// В продакшене удалить эти функции!
// ============================================

// Установить тестовый cookie для разработки
function setTestCookie(telegramId) {
    document.cookie = `telegram_id=${telegramId}; path=/; max-age=2592000`;
    console.log(`Cookie установлен: telegram_id=${telegramId}`);
    location.reload();
}

// Удалить cookie
function clearTestCookie() {
    document.cookie = 'telegram_id=; path=/; max-age=0';
    console.log('Cookie удалён');
    location.reload();
}
