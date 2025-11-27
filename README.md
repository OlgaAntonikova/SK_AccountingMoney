Balance Manager.
A web application for tracking the shared balance of a family or a group of people, with Telegram bot integration.
All members can deposit or withdraw money from the shared account, and all transactions are tracked with the author of each transaction clearly recorded.

Key Features:
* Shared balance — a single balance for all members
* Deposits and withdrawals — any member can manage the shared funds
* Transaction history — full transparency with details on who performed each operation and when
* Telegram integration — access through a Telegram bot
* Simple interface — optimized for quick and easy transactions
* Security — authentication via Telegram ID

Architecture:
* Backend:
ASP.NET Core 8.0 – modern web framework
Entity Framework Core – ORM for interacting with the database
SQLite – lightweight database (easily migratable to MySQL or PostgreSQL)

* Frontend:
Vanilla JavaScript – no frameworks used
CSS3 – modern styling with gradients
Responsive Design – fully adaptable to different screen sizes

* Integration:
Telegram Bot API – used for authentication and access control

* Database:
SQLite – simple and embedded SQL database

* Deployment
Hosted on DigitalOcean Droplet
