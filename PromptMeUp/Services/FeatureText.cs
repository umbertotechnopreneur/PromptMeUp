// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class FeatureText
{
    private static readonly Dictionary<string, string[]> Entries = new(StringComparer.Ordinal)
    {
        ["Script.Help"] = ["Create or revise a PowerShell script", "Crea o modifica uno script PowerShell", "Créer ou modifier un script PowerShell", "PowerShell-Skript erstellen oder überarbeiten", "Crear o modificar un script PowerShell", "Tạo hoặc sửa tập lệnh PowerShell"],
        ["Script.OutputOption"] = ["--output requires one new .ps1 destination.", "--output richiede una nuova destinazione .ps1.", "--output exige une nouvelle destination .ps1.", "--output benötigt ein neues .ps1-Ziel.", "--output requiere un destino .ps1 nuevo.", "--output cần đường dẫn .ps1 mới."],
        ["Script.Usage"] = ["Use --script <request> [--file <source>] [--output <new.ps1>].", "Usa --script <richiesta> [--file <sorgente>] [--output <nuovo.ps1>].", "Utilisez --script <demande> [--file <source>] [--output <nouveau.ps1>].", "Verwenden Sie --script <Anfrage> [--file <Quelle>] [--output <neu.ps1>].", "Usa --script <petición> [--file <origen>] [--output <nuevo.ps1>].", "Dùng --script <yêu-cầu> [--file <nguồn>] [--output <mới.ps1>]."],
        ["Script.Invalid"] = ["Invalid script: require non-empty source up to 12000 characters without credentials or redaction markers.", "Script non valido: serve codice non vuoto, entro 12000 caratteri, senza credenziali o marcatori di redazione.", "Script invalide : code non vide, 12000 caractères maximum, sans identifiants ni marqueurs de masquage.", "Ungültiges Skript: nicht leer, höchstens 12000 Zeichen, ohne Zugangsdaten oder Schwärzungsmarker.", "Script inválido: código no vacío de hasta 12000 caracteres sin credenciales ni marcas de ocultación.", "Tập lệnh không hợp lệ: cần mã không rỗng, tối đa 12000 ký tự, không chứa thông tin xác thực hay dấu đã lọc."],
        ["Script.SaveError"] = ["Cannot save: choose a writable destination that does not already exist.", "Salvataggio impossibile: scegli una destinazione scrivibile che non esista già.", "Échec de sauvegarde : choisissez une destination accessible qui n'existe pas.", "Speichern fehlgeschlagen: Wählen Sie ein beschreibbares, noch nicht vorhandenes Ziel.", "No se puede guardar: elige un destino escribible que no exista.", "Không thể lưu: chọn đường dẫn ghi được và chưa tồn tại."],
        ["Script.Source"] = ["Full source", "Codice completo", "Code complet", "Vollständiger Quelltext", "Código completo", "Toàn bộ mã"],
        ["Script.Action"] = ["Choose the next step", "Scegli il prossimo passo", "Choisissez la prochaine étape", "Nächsten Schritt wählen", "Elige el siguiente paso", "Chọn bước tiếp theo"],
        ["Script.Cancel"] = ["Finish without saving", "Termina senza salvare", "Terminer sans enregistrer", "Ohne Speichern beenden", "Terminar sin guardar", "Kết thúc không lưu"],
        ["Script.Save"] = ["Save to a new file", "Salva in un nuovo file", "Enregistrer dans un nouveau fichier", "In neuer Datei speichern", "Guardar en un archivo nuevo", "Lưu vào tệp mới"],
        ["Script.Validate"] = ["Review the syntax-check command", "Rivedi il comando di controllo sintassi", "Vérifier la commande d'analyse syntaxique", "Befehl zur Syntaxprüfung prüfen", "Revisar el comando de validación sintáctica", "Xem lệnh kiểm tra cú pháp"],
        ["Script.Revise"] = ["Request a revision", "Richiedi una modifica", "Demander une modification", "Überarbeitung anfordern", "Solicitar una modificación", "Yêu cầu chỉnh sửa"],
        ["Script.Confirm"] = ["Save the displayed source to {0}?", "Salvare il codice mostrato in {0}?", "Enregistrer le code affiché dans {0} ?", "Angezeigten Quelltext in {0} speichern?", "¿Guardar el código mostrado en {0}?", "Lưu mã đã hiển thị vào {0}?"],
        ["Script.Destination"] = ["New .ps1 destination", "Nuova destinazione .ps1", "Nouvelle destination .ps1", "Neues .ps1-Ziel", "Nuevo destino .ps1", "Đường dẫn .ps1 mới"],
        ["Script.Saved"] = ["Saved: {0}", "Salvato: {0}", "Enregistré : {0}", "Gespeichert: {0}", "Guardado: {0}", "Đã lưu: {0}"],
        ["Script.ValidationNote"] = ["Syntax checks do not prove correctness or safety. AnalyzerAvailable reports whether optional PSScriptAnalyzer was used.", "La sintassi non dimostra correttezza o sicurezza. AnalyzerAvailable indica se è stato usato PSScriptAnalyzer opzionale.", "La syntaxe ne prouve ni correction ni sécurité. AnalyzerAvailable indique l'utilisation du module facultatif PSScriptAnalyzer.", "Syntaxprüfungen beweisen weder Korrektheit noch Sicherheit. AnalyzerAvailable zeigt die Nutzung des optionalen PSScriptAnalyzer.", "La sintaxis no garantiza corrección ni seguridad. AnalyzerAvailable indica si se usó PSScriptAnalyzer opcional.", "Kiểm tra cú pháp không chứng minh tính đúng đắn hay an toàn. AnalyzerAvailable cho biết có dùng PSScriptAnalyzer tùy chọn hay không."],
        ["Script.Revision"] = ["Describe the change", "Descrivi la modifica", "Décrivez la modification", "Änderung beschreiben", "Describe el cambio", "Mô tả thay đổi"],
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
