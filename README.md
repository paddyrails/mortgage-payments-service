# Payments Service

Microservice for managing payments in the Mortgage Application system.

## ⚠️ Dependencies

This service depends on:
- **Customer Service** (http://localhost:5001)
- **Loans Service** (http://localhost:5003)

## API Endpoints

### Payments
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/payments | Get all payments |
| GET | /api/payments/{id} | Get payment by ID |
| GET | /api/payments/loan/{loanId} | Get payments by loan |
| GET | /api/payments/customer/{customerId} | Get payments by customer |
| GET | /api/payments/loan/{loanId}/history | Get payment history |
| POST | /api/payments | Create payment |
| POST | /api/payments/{id}/process | Process payment |
| POST | /api/payments/{id}/cancel | Cancel payment |

### Payment Schedule
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/payments/schedule/loan/{loanId} | Get schedule |
| POST | /api/payments/schedule | Create schedule |
| PATCH | /api/payments/schedule/loan/{loanId}/autopay | Toggle autopay |

### Late Fees
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/payments/fees/loan/{loanId} | Get late fees |
| POST | /api/payments/fees/loan/{loanId} | Assess late fee |

## Running

```bash
# Start dependencies first!
# Terminal 1: Customer Service (port 5001)
# Terminal 2: Property Service (port 5002)
# Terminal 3: Loans Service (port 5003)

# Then start Payments service
cd src/Payments.API
dotnet run
```

Swagger UI: http://localhost:5004/swagger
