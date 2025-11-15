namespace Haley.Enums {
    public enum PlainAuthMode {
        Basic = 0,
        HeaderAuthToken , //Json Web Token
        HeaderApiKey,
        Cookie, 
        AzureSAML,
        QueryToken,
        FormToken
    }
}
