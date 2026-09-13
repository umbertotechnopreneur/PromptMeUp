// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language costs UI catalog.</summary>
    private static void AddCostsEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Costs.Affordable", new(
            English: "affordable",
            Italian: "accessibile",
            French: "abordable",
            German: "bezahlbar",
            Spanish: "asequible",
            Vietnamese: "hợp lý"));
        entries.Add("Costs.ApiCost", new(
            English: "Organization cost",
            Italian: "Costo organizzazione",
            French: "Coût organisation",
            German: "Organisationskosten",
            Spanish: "Coste de organización",
            Vietnamese: "Chi phí tổ chức"));
        entries.Add("Costs.Cached", new(
            English: "Cached / 1M",
            Italian: "Cache / 1M",
            French: "Cache / 1M",
            German: "Cache / 1M",
            Spanish: "Caché / 1M",
            Vietnamese: "Đã lưu / 1M"));
        entries.Add("Costs.Cheap", new(
            English: "cheap",
            Italian: "economico",
            French: "économique",
            German: "günstig",
            Spanish: "económico",
            Vietnamese: "rẻ"));
        entries.Add("Costs.Extreme", new(
            English: "extreme",
            Italian: "estremo",
            French: "extrême",
            German: "extrem",
            Spanish: "extremo",
            Vietnamese: "rất cao"));
        entries.Add("Costs.Input", new(
            English: "Input / 1M",
            Italian: "Input / 1M",
            French: "Entrée / 1M",
            German: "Eingabe / 1M",
            Spanish: "Entrada / 1M",
            Vietnamese: "Đầu vào / 1M"));
        entries.Add("Costs.LastSync", new(
            English: "Last price sync",
            Italian: "Ultima sincronizzazione",
            French: "Dernière synchronisation",
            German: "Letzte Preissynchronisierung",
            Spanish: "Última sincronización",
            Vietnamese: "Đồng bộ giá gần nhất"));
        entries.Add("Costs.Model", new(
            English: "Model",
            Italian: "Modello",
            French: "Modèle",
            German: "Modell",
            Spanish: "Modelo",
            Vietnamese: "Mô hình"));
        entries.Add("Costs.Models", new(
            English: "Models",
            Italian: "Modelli",
            French: "Modèle",
            German: "Modelle",
            Spanish: "Modelos",
            Vietnamese: "Mô hình"));
        entries.Add("Costs.MonthEstimate", new(
            English: "Month estimate",
            Italian: "Stima mese",
            French: "Estimation du mois",
            German: "Monatsschätzung",
            Spanish: "Estimación mensual",
            Vietnamese: "Ước tính tháng"));
        entries.Add("Costs.Output", new(
            English: "Output / 1M",
            Italian: "Output / 1M",
            French: "Sortie / 1M",
            German: "Ausgabe / 1M",
            Spanish: "Salida / 1M",
            Vietnamese: "Đầu ra / 1M"));
        entries.Add("Costs.Overview", new(
            English: "Cost snapshot",
            Italian: "Specchietto costi",
            French: "Aperçu des coûts",
            German: "Kostenübersicht",
            Spanish: "Resumen de costes",
            Vietnamese: "Tóm tắt chi phí"));
        entries.Add("Costs.Premium", new(
            English: "premium",
            Italian: "premium",
            French: "premium",
            German: "premium",
            Spanish: "premium",
            Vietnamese: "cao cấp"));
        entries.Add("Costs.Refreshed", new(
            English: "Cost data refreshed.",
            Italian: "Dati di costo aggiornati.",
            French: "Données de coût actualisées.",
            German: "Kostendaten aktualisiert.",
            Spanish: "Datos de costes actualizados.",
            Vietnamese: "Đã cập nhật dữ liệu chi phí."));
        entries.Add("Costs.Refreshing", new(
            English: "Refreshing OpenAI pricing and cost data...",
            Italian: "Aggiornamento prezzi e costi OpenAI...",
            French: "Actualisation des prix et coûts OpenAI...",
            German: "OpenAI-Preise und Kosten werden aktualisiert...",
            Spanish: "Actualizando precios y costes de OpenAI...",
            Vietnamese: "Đang cập nhật giá và chi phí OpenAI..."));
        entries.Add("Costs.Requests", new(
            English: "Requests today",
            Italian: "Richieste oggi",
            French: "Requêtes aujourd'hui",
            German: "Anfragen heute",
            Spanish: "Solicitudes hoy",
            Vietnamese: "Yêu cầu hôm nay"));
        entries.Add("Costs.Subtitle", new(
            English: "Standard prices per 1M tokens, local estimates, and optional organization cost data.",
            Italian: "Prezzi standard per 1M token, stime locali e costi organizzazione opzionali.",
            French: "Prix standard par million de jetons, estimations locales et coûts d'organisation facultatifs.",
            German: "Standardpreise pro 1 Mio. Token, lokale Schätzungen und optionale Organisationskosten.",
            Spanish: "Precios estándar por 1 millón de tokens, estimaciones locales y costes de organización opcionales.",
            Vietnamese: "Giá chuẩn trên 1 triệu token, ước tính cục bộ và chi phí tổ chức tùy chọn."));
        entries.Add("Costs.Tier", new(
            English: "Cost tier",
            Italian: "Fascia costo",
            French: "Niveau de coût",
            German: "Kostenstufe",
            Spanish: "Nivel de coste",
            Vietnamese: "Mức chi phí"));
        entries.Add("Costs.Title", new(
            English: "OpenAI costs",
            Italian: "Costi OpenAI",
            French: "Coûts OpenAI",
            German: "OpenAI-Kosten",
            Spanish: "Costes de OpenAI",
            Vietnamese: "Chi phí OpenAI"));
        entries.Add("Costs.TodayEstimate", new(
            English: "Today estimate",
            Italian: "Stima oggi",
            French: "Estimation aujourd'hui",
            German: "Schätzung heute",
            Spanish: "Estimación de hoy",
            Vietnamese: "Ước tính hôm nay"));
        entries.Add("Costs.Tokens", new(
            English: "Tokens today",
            Italian: "Token oggi",
            French: "Jetons aujourd'hui",
            German: "Token heute",
            Spanish: "Tokens hoy",
            Vietnamese: "Token hôm nay"));
        entries.Add("Costs.Unavailable", new(
            English: "Not available",
            Italian: "Non disponibile",
            French: "Indisponible",
            German: "Nicht verfügbar",
            Spanish: "No disponible",
            Vietnamese: "Không có"));
    }
}
