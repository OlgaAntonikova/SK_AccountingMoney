let currentUser = null;

const tg = window.Telegram.WebApp;
let authToken = localStorage.getItem('authToken');

// Initializing the Application
document.addEventListener('DOMContentLoaded', async () => {
    await checkAuth();
});

// Authentication check
async function checkAuth() {
    try {
        if (!authToken) {
            const initData = tg.initData || '';
            if (!initData) {
                console.error('No Telegram initData');
                return;
            }

            // Telegram auth
            const authResponse = await fetch('/api/auth/telegram', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`
                },
                body: JSON.stringify({ initData })
            });

            const authData = await authResponse.json();
            if (!authResponse.ok) {
                console.error('Auth error:', authData.error);
                return;
            }

            authToken = authData.token;
            localStorage.setItem('authToken', authToken);
        }

        const response = await fetch('/api/auth/check', {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        const data = await response.json();

        if (data.authenticated) {
            currentUser = data.user;
            showMainScreen();
            await loadUserData();
        }
        else {
            // Token is invalid, clear
            console.error('Token is invalid!!!')
            localStorage.removeItem('authToken');
            authToken = null;            
        }
    }
    catch (error) {
        console.error('Authentication check error:', error);
    }
}



// Loading user data
async function loadUserData() {
    await Promise.all([
        loadBalance(),
        loadTransactions()
    ]);

    document.getElementById('userName').textContent =
        `👤 ${currentUser.userName || 'User'} (ID: ${currentUser.telegramId})`;
}

// Loading balance
async function loadBalance() {
    try {
        const response = await fetch('/api/balance', {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        const data = await response.json();

        const balanceElement = document.getElementById('balanceAmount');
        balanceElement.textContent = `${data.balance.toFixed(2)} €`;

        // Balance change animation
        balanceElement.classList.add('balance-update');
        setTimeout(() => balanceElement.classList.remove('balance-update'), 500);
    } catch (error) {
        console.error('Balance loading error:', error);
    }
}

// Downloading transaction history
async function loadTransactions() {
    try {
        const response = await fetch('/api/balance/transactions?limit=20', {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        const transactions = await response.json();

        const listElement = document.getElementById('transactionsList');

        if (transactions.length === 0) {
            listElement.innerHTML = '<p class="empty">The transaction history is empty</p>';
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
                            <p class="transaction-description">${t.description || 'No description'}${userInfo}</p>
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
        console.error('Error loading transactions:', error);
        document.getElementById('transactionsList').innerHTML =
            '<p class="error">Error loading history</p>';
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

// Balance replenishment
async function deposit() {
    const amount = parseFloat(document.getElementById('depositAmount').value);
    const description = document.getElementById('depositDescription').value;

    if (!amount || amount <= 0) {
        showNotification('Please enter the correct amount', 'error');
        return;
    }

    try {
        const response = await fetch('/api/balance/deposit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({
                amount: amount,
                description: description || 'Balance replenishment'
            })
        });

        const data = await response.json();

        if (response.ok) {
            showNotification('The balance has been successfully replenished!', 'success');
            closeDepositModal();
            document.getElementById('depositAmount').value = '';
            document.getElementById('depositDescription').value = '';
            await loadUserData();
        } else {
            showNotification(data.error || 'Replenishment error', 'error');
        }
    } catch (error) {
        console.error('Replenishment error:', error);
        showNotification('Error connecting to the server', 'error');
    }
}

// Withdrawal of funds
async function withdraw() {
    const amount = parseFloat(document.getElementById('withdrawAmount').value);
    const description = document.getElementById('withdrawDescription').value;

    if (!amount || amount <= 0) {
        showNotification('Please enter the correct amount', 'error');
        return;
    }

    try {
        const response = await fetch('/api/balance/withdraw', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({
                amount: amount,
                description: description || 'Withdrawal of funds'
            })
        });

        const data = await response.json();

        if (response.ok) {
            showNotification('Funds have been successfully withdrawn!', 'success');
            document.getElementById('withdrawAmount').value = '';
            document.getElementById('withdrawDescription').value = '';
            await loadUserData();
        } else {
            showNotification(data.error || 'Removal error', 'error');
        }
    } catch (error) {
        console.error('Removal error:', error);
        showNotification('Error connecting to the server', 'error');
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

    if (diff < 60000) return 'just now';
    if (diff < 3600000) return `${Math.floor(diff / 60000)} min ago`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)} h ago`;

    return date.toLocaleDateString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}