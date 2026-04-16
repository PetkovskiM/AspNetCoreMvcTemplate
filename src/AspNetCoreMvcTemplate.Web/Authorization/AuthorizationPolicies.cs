namespace AspNetCoreMvcTemplate.Web.Authorization
{
    // Centralna lokacija za site imeto na politikite za avtorizacija.
    // Se koristi kako konstanti za da se izbegnat magicni stringovi niz kontrolerite.
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string ActiveUser = "ActiveUser";
    }
}
