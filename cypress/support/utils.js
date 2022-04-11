export function getTestVariables(prefix) {
  return {
    transactionIdEnv: `${prefix}-transactionIdEnv`,
    paymentTransactionIdEnv: `${prefix}-tidEnv`,
  }
}
