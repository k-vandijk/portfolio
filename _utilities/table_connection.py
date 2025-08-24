from dotenv import load_dotenv
import os
from azure.data.tables import TableServiceClient
load_dotenv()

# Get client
service_client = TableServiceClient.from_connection_string(conn_str=os.getenv("TRANSACTIONS_TABLE_CONNECTION_STRING")  )
table_client = service_client.get_table_client("transactions")

# Get highest row key
entities = table_client.query_entities("PartitionKey eq 'transactions'")
max_row_key = max(
    (int(e["RowKey"]) for e in entities if e["RowKey"].isdigit()),
    default=0
)
next_row_key = str(max_row_key + 1)

# Get csv content from './_assets/transactions_cleaned.csv'
csv_content = ""
with open('./_assets/financien_cleaned.csv', 'r') as f:
    csv_content = f.read()

# Create entities
for line in csv_content.splitlines()[1:]:
    date, ticker, amount, purchase_price, transaction_costs, total_costs = line.split(';')
    entity = {
        "PartitionKey": "transactions",
        "RowKey": next_row_key,
        "Date": date,
        "Ticker": ticker,
        "Amount": amount,
        "PurchasePrice": purchase_price,
        "TransactionCosts": transaction_costs,
    }

    table_client.create_entity(entity=entity)
    print("Entity inserted.")

    next_row_key = str(int(next_row_key) + 1)