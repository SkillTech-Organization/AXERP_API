-- Deletes all data from database
-- TransactionStatuses is not emptied because it's seeded by the db creation script.

-- Has foreign key for Transactions, need to be cleared first

delete from TruckCompanyToDelivery
delete from CustomerToDelivery

-- Has foreign key for other tables

delete from Transactions

-- Doesn't have foreign keys towards transactions or dictionary tables

delete from Documents
delete from Entities
delete from Interfaces		-- Foreign key towards Points
delete from Points
delete from TruckCompanies
