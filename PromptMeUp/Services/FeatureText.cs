// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class FeatureText
{
    private static readonly Dictionary<string, string[]> Entries = new(StringComparer.Ordinal)
    {
        ["Diagnose.Help"] = ["Diagnose errors and logs", "Diagnostica errori e log", "Diagnostiquer les erreurs et journaux", "Fehler und Protokolle diagnostizieren", "Diagnosticar errores y registros", "Chẩn đoán lỗi và nhật ký"],
        ["Input.FileOption"] = ["--file requires one path and may appear once.", "--file richiede un percorso e può apparire una sola volta.", "--file exige un chemin et ne peut apparaître qu'une fois.", "--file benötigt einen Pfad und darf nur einmal vorkommen.", "--file requiere una ruta y solo puede aparecer una vez.", "--file cần một đường dẫn và chỉ được dùng một lần."],
        ["Input.SourceConflict"] = ["Choose one input source: text, --file, or a pipe.", "Scegli una fonte: testo, --file o una pipe.", "Choisissez une source : texte, --file ou un pipe.", "Wählen Sie eine Quelle: Text, --file oder Pipe.", "Elige una fuente: texto, --file o una tubería.", "Chọn một nguồn: văn bản, --file hoặc pipe."],
        ["Input.FileError"] = ["Cannot read this text file.", "Impossibile leggere questo file di testo.", "Impossible de lire ce fichier texte.", "Diese Textdatei kann nicht gelesen werden.", "No se puede leer este archivo de texto.", "Không thể đọc tệp văn bản này."],
        ["Input.Empty"] = ["Provide non-empty input.", "Fornisci un testo non vuoto.", "Fournissez un texte non vide.", "Geben Sie einen nicht leeren Text ein.", "Proporciona texto no vacío.", "Cung cấp văn bản không rỗng."],
        ["Input.TooLong"] = ["Input exceeds {0} characters. Select a smaller excerpt.", "Il testo supera {0} caratteri. Seleziona un estratto più breve.", "Le texte dépasse {0} caractères. Sélectionnez un extrait plus court.", "Die Eingabe überschreitet {0} Zeichen. Wählen Sie einen kürzeren Ausschnitt.", "El texto supera {0} caracteres. Selecciona un fragmento menor.", "Đầu vào vượt quá {0} ký tự. Chọn đoạn ngắn hơn."],
        ["Input.Timeout"] = ["Input did not finish within 30 seconds.", "L'input non è terminato entro 30 secondi.", "La lecture n'est pas terminée après 30 secondes.", "Die Eingabe wurde nicht innerhalb von 30 Sekunden abgeschlossen.", "La entrada no terminó en 30 segundos.", "Đầu vào chưa kết thúc sau 30 giây."],
        ["Input.SecretArgument"] = ["Do not pass credentials as command-line arguments. Use a sanitized file or interactive input.", "Non passare credenziali negli argomenti. Usa un file redatto o l'input interattivo.", "Ne transmettez pas d'identifiants en arguments. Utilisez un fichier expurgé ou la saisie interactive.", "Übergeben Sie keine Zugangsdaten als Argumente. Verwenden Sie eine bereinigte Datei oder interaktive Eingabe.", "No pases credenciales como argumentos. Usa un archivo depurado o entrada interactiva.", "Không truyền thông tin xác thực qua đối số. Dùng tệp đã lọc hoặc nhập tương tác."],
        ["Input.Sharing"] = ["The selected text is sent to the AI provider after recognizable credentials are redacted.", "Il testo selezionato viene inviato al provider AI dopo la redazione delle credenziali riconoscibili.", "Le texte sélectionné est envoyé au fournisseur IA après masquage des identifiants reconnaissables.", "Der ausgewählte Text wird nach Entfernung erkennbarer Zugangsdaten an den KI-Anbieter gesendet.", "El texto seleccionado se envía al proveedor de IA tras ocultar las credenciales reconocibles.", "Văn bản đã chọn được gửi đến nhà cung cấp AI sau khi lọc thông tin xác thực nhận diện được."]
    };

    /// <summary>Resolves a feature string from the complete six-language catalog.</summary>
    internal static bool TryGet(string key, string language, out string value)
    {
        if (!Entries.TryGetValue(key, out var translations))
        {
            value = string.Empty;
            return false;
        }
        var index = language switch { "en" => 0, "it" => 1, "fr" => 2, "de" => 3, "es" => 4, "vi" => 5, _ => throw new ArgumentOutOfRangeException(nameof(language)) };
        value = translations[index];
        return true;
    }
}
