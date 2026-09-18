namespace SupersonicWisdomSDK
{
    public static class SwNetworkConstants
    {
        internal enum HttpStatusCode
        {
            Ok = 200,
            Created = 201,
            Accepted = 202,
            NoContent = 204,
            MultipleChoices = 300,
            MovedPermanently = 301,
            Found = 302,
            NotModified = 304,
            BadRequest = 400,
            Unauthorized = 401,
            Forbidden = 403,
            NotFound = 404,
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeout = 504,
            HttpVersionNotSupported = 505,
        }
    }
}