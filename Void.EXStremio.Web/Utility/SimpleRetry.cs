namespace Void.EXStremio.Web.Utility {
    public static class SimpleRetry {
        public static async Task<T> Retry<T>(Func<Task<T>> action, Func<T, bool> until, int count = 3, int retryInterval = 60, bool failOnException = true) {
            for (var i = 0; i < count; i++) {
                try {
                    var result = await action();
                    if (until(result)) {
                        return result;
                    }
                } catch {
                    if (failOnException) { throw; }
                    // TODO: logging?
                }

                await Task.Delay(retryInterval * 1000);
            }

            throw new InvalidOperationException("Retry failed");
        }
    }
}
