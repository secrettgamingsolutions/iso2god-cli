# Corrections apportées pour la recherche de titres Xbox 360

## Problème identifié

Le système ne trouvait pas les titres de jeux ni via l'API XboxUnity ni dans la base CSV locale, même quand le jeu était présent dans le CSV.

### Exemple de log d'erreur
```
+ Looking up game title online (TitleID: 4541099F)...
+ Attempting to fetch game title from XboxUnity API...
+ Online lookup failed or timed out. Trying local CSV database...
+ Game title not found in local CSV database.
+ Using TitleID as game name: 4541099F
```

## Cause racine

### 1. Bug dans le parseur CSV (GameListCsvReader.cs)

**Problème** : Les lignes du fichier CSV sont entièrement entourées de guillemets :
```csv
"4541099F	6AE873E7	Zuma's Revenge!"
```

L'ancien code faisait :
```csharp
string[] parts = line.Split('\t');
string csvTitleId = parts[0].Trim().Trim('"');  // ? Ne marche pas !
```

Résultat : `parts[0]` contenait `"4541099F` au lieu de `4541099F`, donc la comparaison échouait.

**Solution** : Retirer les guillemets externes AVANT de splitter :
```csharp
// Remove outer quotes if present
line = line.Trim();
if (line.StartsWith("\"") && line.EndsWith("\""))
{
    line = line.Substring(1, line.Length - 2);
}

// Now split - parts will be clean
string[] parts = line.Split('\t');
string csvTitleId = parts[0].Trim();  // ? Fonctionne !
```

### 2. Améliorations de l'API XboxUnity (XboxUnityScraper.cs)

**Améliorations apportées** :
1. ? Augmentation du timeout de 5s à 10s (connexions lentes)
2. ? Ajout de logs d'erreur détaillés pour le debug
3. ? Amélioration du parsing JSON pour vérifier si l'array `Items` est vide
4. ? Meilleure gestion des exceptions avec messages explicites

```csharp
// Vérification que l'array Items n'est pas vide
int arrayStart = json.IndexOf("[", itemsIndex);
int arrayEnd = json.IndexOf("]", arrayStart);
if (arrayEnd == -1 || arrayEnd - arrayStart <= 1)
{
    // Empty array - no results
    return null;
}
```

## Fichiers modifiés

1. **Chilano\Iso2God\GameListCsvReader.cs**
   - Correction du parsing des lignes CSV quotées
   - Suppression des guillemets externes avant le split

2. **Chilano\Iso2God\XboxUnityScraper.cs**
   - Timeout augmenté à 10 secondes
   - Ajout de logs d'erreur détaillés
   - Amélioration du parsing JSON
   - Vérification de l'array Items vide

## Test de validation

Pour tester avec le TitleID `4541099F` (Zuma's Revenge!) :

```bash
.\bin\iso2god.exe "path\to\Zuma's Revenge.iso" "output\folder"
```

Le système devrait maintenant afficher :
```
+ Game title found in local CSV: Zuma's Revenge!
```

Au lieu de :
```
+ Game title not found in local CSV database.
+ Using TitleID as game name: 4541099F
```

## Conformité avec l'API XboxUnity

L'implémentation respecte la documentation officielle :
- URL : `http://xboxunity.net/Resources/Lib/TitleList.php`
- Paramètres requis : `page`, `count`, `search`, `sort`, `direction`, `category`, `filter`
- Format de réponse : JSON avec structure `{ "Items": [...], "Count": N, ... }`

Référence : https://github.com/UncreativeXenon/XboxUnity-Scraper
