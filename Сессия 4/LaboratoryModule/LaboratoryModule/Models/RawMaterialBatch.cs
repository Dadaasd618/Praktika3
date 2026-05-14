using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaboratoryModule.Models
{
    public class RawMaterialBatch
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonPropertyName("rawMaterialId")]
        public long RawMaterialId { get; set; }

        [JsonPropertyName("supplierName")]
        public string SupplierName { get; set; }

        [JsonPropertyName("quantityKg")]
        public decimal QuantityKg { get; set; }

        [JsonPropertyName("arrivalDate")]
        public DateTime ArrivalDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("storageLocation")]
        public string StorageLocation { get; set; }

        public string RawMaterialName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public List<LabTest> Tests { get; set; }
    }
}