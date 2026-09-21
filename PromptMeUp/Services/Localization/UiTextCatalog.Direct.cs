// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds direct-mode status, countdown and refusal messages in all six supported languages.</summary>
    private static void AddDirectEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Direct.Setting", new(
            English: "Direct execution (5-second countdown)",
            Italian: "Esecuzione direct (countdown di 5 secondi)",
            French: "Exécution directe (délai de 5 secondes)",
            German: "Direkte Ausführung (5 Sekunden Countdown)",
            Spanish: "Ejecución directa (cuenta atrás de 5 segundos)",
            Vietnamese: "Thực thi trực tiếp (đếm ngược 5 giây)"));
        entries.Add("Direct.SettingHelp", new(
            English: "On by default for questions and chat. Requires local and AI risk checks. Turn off to confirm each command manually; --direct overrides this setting for one session.",
            Italian: "Attiva di default per domande e chat. Richiede controlli del rischio locali e AI. Disattivala per confermare ogni comando; --direct la riattiva per una sessione.",
            French: "Activée par défaut pour les questions et le chat. Contrôles locaux et IA obligatoires. Désactivez pour confirmer chaque commande ; --direct la réactive pour une session.",
            German: "Standardmäßig für Fragen und Chat aktiv. Lokale und KI-Risikoprüfung erforderlich. Ausschalten für manuelle Bestätigung; --direct aktiviert sie für eine Sitzung.",
            Spanish: "Activa por defecto para preguntas y chat. Requiere controles locales y de IA. Desactívala para confirmar cada comando; --direct la activa para una sesión.",
            Vietnamese: "Mặc định bật cho câu hỏi và trò chuyện. Cần kiểm tra rủi ro cục bộ và AI. Tắt để xác nhận từng lệnh; --direct bật lại cho một phiên."));
        entries.Add("Direct.RequireConfirmation", new(
            English: "Require confirmation before executing commands",
            Italian: "Richiedi conferma prima di eseguire i comandi",
            French: "Demander confirmation avant d'exécuter les commandes",
            German: "Bestätigung vor dem Ausführen von Befehlen verlangen",
            Spanish: "Solicitar confirmación antes de ejecutar comandos",
            Vietnamese: "Yêu cầu xác nhận trước khi chạy lệnh"));
        entries.Add("Direct.RequireConfirmationHelp", new(
            English: "When on, each eligible command asks for manual confirmation. Turn it off to use the five-second direct countdown; --direct overrides this setting for one session.",
            Italian: "Se attivo, ogni comando idoneo richiede una conferma manuale. Disattivalo per usare il countdown direct di cinque secondi; --direct lo sovrascrive per una sessione.",
            French: "Lorsqu'il est activé, chaque commande admissible demande une confirmation manuelle. Désactivez-le pour utiliser le délai direct de cinq secondes ; --direct le remplace pour une session.",
            German: "Wenn aktiviert, verlangt jeder zulässige Befehl eine manuelle Bestätigung. Ausschalten aktiviert den direkten Fünf-Sekunden-Countdown; --direct überschreibt dies für eine Sitzung.",
            Spanish: "Cuando está activado, cada comando apto solicita confirmación manual. Desactívalo para usar la cuenta atrás directa de cinco segundos; --direct lo anula durante una sesión.",
            Vietnamese: "Khi bật, mỗi lệnh hợp lệ đều yêu cầu xác nhận thủ công. Tắt để dùng đếm ngược trực tiếp năm giây; --direct ghi đè cho một phiên."));
        entries.Add("Direct.Usage", new(
            English: "Use hm --direct \"request\". Specify --direct only once, with a non-empty request.",
            Italian: "Usa hm --direct \"richiesta\". Specifica --direct una sola volta, con una richiesta non vuota.",
            French: "Utilisez hm --direct \"demande\". Indiquez --direct une seule fois, avec une demande non vide.",
            German: "Verwende hm --direct \"Anfrage\". Gib --direct nur einmal mit einer nicht leeren Anfrage an.",
            Spanish: "Usa hm --direct \"solicitud\". Indica --direct una sola vez, con una solicitud no vacía.",
            Vietnamese: "Dùng hm --direct \"yêu cầu\". Chỉ dùng --direct một lần với yêu cầu không rỗng."));
        entries.Add("Direct.Help", new(
            English: "Review commands locally and with AI, run after a five-second countdown, then analyze the output. High or critical risk blocks execution.",
            Italian: "Valuta i comandi localmente e con l’AI, li esegue dopo un countdown di cinque secondi e analizza l’output. Il rischio alto o critico blocca l’esecuzione.",
            French: "Évalue les commandes localement et avec l’IA, les exécute après cinq secondes et analyse le résultat. Un risque élevé ou critique bloque l’exécution.",
            German: "Prüft Befehle lokal und mit KI, führt sie nach fünf Sekunden aus und analysiert die Ausgabe. Hohes oder kritisches Risiko blockiert die Ausführung.",
            Spanish: "Evalúa los comandos localmente y con IA, los ejecuta tras cinco segundos y analiza el resultado. Un riesgo alto o crítico bloquea la ejecución.",
            Vietnamese: "Đánh giá lệnh cục bộ và bằng AI, chạy sau năm giây rồi phân tích kết quả. Rủi ro cao hoặc nghiêm trọng sẽ chặn thực thi."));
        entries.Add("Direct.Active", new(
            English: "Direct mode is active for this conversation. Each eligible command runs after 5 seconds. Enter runs now; Esc or Ctrl+C cancels.",
            Italian: "Modalità direct attiva per questa conversazione. Ogni comando ammesso parte dopo 5 secondi. Invio esegue subito; Esc o Ctrl+C annulla.",
            French: "Mode direct actif pour cette conversation. Chaque commande admise démarre après 5 secondes. Entrée lance immédiatement ; Échap ou Ctrl+C annule.",
            German: "Direktmodus für dieses Gespräch aktiv. Jeder zulässige Befehl startet nach 5 Sekunden. Eingabe startet sofort; Esc oder Strg+C bricht ab.",
            Spanish: "Modo directo activo en esta conversación. Cada comando permitido se ejecuta tras 5 segundos. Intro ejecuta ahora; Esc o Ctrl+C cancela.",
            Vietnamese: "Chế độ direct đang bật cho cuộc trò chuyện này. Mỗi lệnh được phép chạy sau 5 giây. Enter chạy ngay; Esc hoặc Ctrl+C hủy."));
        entries.Add("Direct.Countdown", new(
            English: "Execution in {0} seconds",
            Italian: "Esecuzione tra {0} secondi",
            French: "Exécution dans {0} secondes",
            German: "Ausführung in {0} Sekunden",
            Spanish: "Ejecución en {0} segundos",
            Vietnamese: "Thực thi sau {0} giây"));
        entries.Add("Direct.Keys", new(
            English: "Enter runs now · Esc cancels · Ctrl+C cancels",
            Italian: "Invio esegue subito · Esc annulla · Ctrl+C annulla",
            French: "Entrée lance maintenant · Échap annule · Ctrl+C annule",
            German: "Eingabe startet sofort · Esc bricht ab · Strg+C bricht ab",
            Spanish: "Intro ejecuta ahora · Esc cancela · Ctrl+C cancela",
            Vietnamese: "Enter chạy ngay · Esc hủy · Ctrl+C hủy"));
        entries.Add("Direct.Blocked", new(
            English: "Command blocked: direct mode refuses high, critical or unknown risk. No command was executed.",
            Italian: "Comando bloccato: la modalità direct rifiuta rischi alti, critici o sconosciuti. Nessun comando eseguito.",
            French: "Commande bloquée : le mode direct refuse les risques élevés, critiques ou inconnus. Aucune commande exécutée.",
            German: "Befehl blockiert: Der Direktmodus lehnt hohes, kritisches oder unbekanntes Risiko ab. Kein Befehl wurde ausgeführt.",
            Spanish: "Comando bloqueado: el modo directo rechaza riesgos altos, críticos o desconocidos. No se ejecutó ningún comando.",
            Vietnamese: "Đã chặn lệnh: chế độ direct từ chối rủi ro cao, nghiêm trọng hoặc chưa rõ. Không có lệnh nào được chạy."));
        entries.Add("Direct.ReviewRequired", new(
            English: "Command blocked: direct mode requires a successful AI risk review in addition to local checks. No command was executed.",
            Italian: "Comando bloccato: la modalità direct richiede una revisione AI riuscita oltre ai controlli locali. Nessun comando eseguito.",
            French: "Commande bloquée : le mode direct exige une analyse IA réussie en plus des contrôles locaux. Aucune commande exécutée.",
            German: "Befehl blockiert: Der Direktmodus erfordert zusätzlich zur lokalen Prüfung eine erfolgreiche KI-Risikoprüfung. Kein Befehl wurde ausgeführt.",
            Spanish: "Comando bloqueado: el modo directo requiere una revisión de riesgo de IA válida además de los controles locales. No se ejecutó ningún comando.",
            Vietnamese: "Đã chặn lệnh: chế độ direct cần AI đánh giá rủi ro thành công bên cạnh kiểm tra cục bộ. Không có lệnh nào được chạy."));
        entries.Add("Direct.StepLimit", new(
            English: "Direct mode stopped after 8 commands for this request. Review the results before sending another message.",
            Italian: "Modalità direct fermata dopo 8 comandi per questa richiesta. Verifica i risultati prima di inviare un altro messaggio.",
            French: "Mode direct arrêté après 8 commandes pour cette demande. Vérifiez les résultats avant d’envoyer un autre message.",
            German: "Direktmodus nach 8 Befehlen für diese Anfrage angehalten. Prüfe die Ergebnisse vor einer weiteren Nachricht.",
            Spanish: "Modo directo detenido tras 8 comandos para esta solicitud. Revisa los resultados antes de enviar otro mensaje.",
            Vietnamese: "Chế độ direct dừng sau 8 lệnh cho yêu cầu này. Kiểm tra kết quả trước khi gửi tin nhắn tiếp theo."));
    }
}
