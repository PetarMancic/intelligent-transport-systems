namespace WebApplication1.Constants
{
    public static class SqlQueries
    {
        public const string sql = @"
            -- 1. Cela linija (A → G)
            SELECT
                v.""Id"" AS ""VoznjaId"",
                v.""PocetniGrad"" AS ""Polaziste"",
                v.""KrajnjiGrad"" AS ""Odrediste"",
                v.""VremePolaska"" AS ""VremeOd"",
                v.""VremeDolaska"" AS ""VremeDo"",
                (SELECT STRING_AGG(us.""Stanica"", ',' ORDER BY us.""VremeDolaska"")
                 FROM usputne_stanice us WHERE us.""VoznjaId"" = v.""Id"") AS ""StaniceSve""
            FROM voznje v
            WHERE v.""VremePolaska"" >= @odVremena

            UNION ALL

            -- 2. Od početnog grada do usputne (A → E)
            SELECT
                v.""Id"",
                v.""PocetniGrad"",
                us.""Stanica"" AS ""Odrediste"",
                v.""VremePolaska"",
                us.""VremeDolaska"",
                (SELECT STRING_AGG(us2.""Stanica"", ',' ORDER BY us2.""VremeDolaska"")
                 FROM usputne_stanice us2
                 WHERE us2.""VoznjaId"" = v.""Id"" AND us2.""VremeDolaska"" < us.""VremeDolaska"") AS ""StaniceSve""
            FROM voznje v
            JOIN usputne_stanice us ON v.""Id"" = us.""VoznjaId""
            WHERE v.""VremePolaska"" >= @odVremena

            UNION ALL

            -- 3. Od usputne do krajnjeg (E → G)
            SELECT
                v.""Id"",
                us.""Stanica"" AS ""Polaziste"",
                v.""KrajnjiGrad"",
                us.""VremeDolaska"",
                v.""VremeDolaska"",
                (SELECT STRING_AGG(us2.""Stanica"", ',' ORDER BY us2.""VremeDolaska"")
                 FROM usputne_stanice us2
                 WHERE us2.""VoznjaId"" = v.""Id"" AND us2.""VremeDolaska"" > us.""VremeDolaska"") AS ""StaniceSve""
            FROM voznje v
            JOIN usputne_stanice us ON v.""Id"" = us.""VoznjaId""
            WHERE us.""VremeDolaska"" >= @odVremena

            UNION ALL

            -- 4. Između dve usputne stanice (E → F)
            SELECT
                v.""Id"",
                us1.""Stanica"" AS ""Polaziste"",
                us2.""Stanica"" AS ""Odrediste"",
                us1.""VremeDolaska"",
                us2.""VremeDolaska"",
                (SELECT STRING_AGG(us3.""Stanica"", ',' ORDER BY us3.""VremeDolaska"")
                 FROM usputne_stanice us3
                 WHERE us3.""VoznjaId"" = v.""Id""
                   AND us3.""VremeDolaska"" > us1.""VremeDolaska""
                   AND us3.""VremeDolaska"" < us2.""VremeDolaska"") AS ""StaniceSve""
            FROM voznje v
            JOIN usputne_stanice us1 ON v.""Id"" = us1.""VoznjaId""
            JOIN usputne_stanice us2 ON v.""Id"" = us2.""VoznjaId"" AND us2.""VremeDolaska"" > us1.""VremeDolaska""
            WHERE us1.""VremeDolaska"" >= @odVremena";
    }
}
