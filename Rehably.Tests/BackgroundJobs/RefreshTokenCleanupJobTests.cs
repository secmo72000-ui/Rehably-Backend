// NOTE: RefreshTokenCleanupJob tests require integration test with real database
// because ApplicationDbContext.RefreshTokens is not virtual and cannot be mocked
// These tests should be moved to integration test suite

// Integration tests needed for:
// - ExecuteAsync_DeletesRevokedTokensOlderThan7Days
// - ExecuteAsync_DeletesExpiredTokensOlderThan7Days
// - ExecuteAsync_DoesNotDeleteActiveTokens
// - ExecuteAsync_WhenNoTokensToDelete_DoesNotCallSaveChanges
// - ExecuteAsync_MultipleDeletions_LogsCorrectCount
// - ExecuteAsync_CallsSaveChangesOnce_BulkDelete
