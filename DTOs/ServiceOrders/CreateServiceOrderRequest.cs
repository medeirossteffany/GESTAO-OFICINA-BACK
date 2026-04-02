using System.ComponentModel.DataAnnotations;

namespace GestaoOficina.DTOs.ServiceOrders
{
    public class CreateServiceOrderRequest : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int UnitId { get; set; }

        [Range(1, int.MaxValue)]
        public int VehicleId { get; set; }

        [Range(1, int.MaxValue)]
        public int OwnerCustomerId { get; set; }

        [Required]
        public DateTime? EntryDate { get; set; }

        [Required]
        public DateTime? EstimatedDeliveryDate { get; set; }

        public string? BodyworkDescription { get; set; }
        public decimal? BodyworkValue { get; set; }

        public string? PaintDescription { get; set; }
        public decimal? PaintValue { get; set; }

        public string? MechanicsDescription { get; set; }
        public decimal? MechanicsValue { get; set; }

        public List<CreateServiceOrderPartItemRequest>? Parts { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasBodyworkDescription = !string.IsNullOrWhiteSpace(BodyworkDescription);
            var hasBodyworkValue = BodyworkValue is > 0;
            var hasPaintDescription = !string.IsNullOrWhiteSpace(PaintDescription);
            var hasPaintValue = PaintValue is > 0;
            var hasMechanicsDescription = !string.IsNullOrWhiteSpace(MechanicsDescription);
            var hasMechanicsValue = MechanicsValue is > 0;
            var hasParts = Parts is { Count: > 0 };

            if (hasBodyworkDescription != hasBodyworkValue)
                yield return new ValidationResult("Funilaria deve informar descrição e valor juntos.", [nameof(BodyworkDescription), nameof(BodyworkValue)]);

            if (hasPaintDescription != hasPaintValue)
                yield return new ValidationResult("Pintura deve informar descrição e valor juntos.", [nameof(PaintDescription), nameof(PaintValue)]);

            if (hasMechanicsDescription != hasMechanicsValue)
                yield return new ValidationResult("Mecânica deve informar descrição e valor juntos.", [nameof(MechanicsDescription), nameof(MechanicsValue)]);

            var hasBodyworkComplete = hasBodyworkDescription && hasBodyworkValue;
            var hasPaintComplete = hasPaintDescription && hasPaintValue;
            var hasMechanicsComplete = hasMechanicsDescription && hasMechanicsValue;

            if (!hasBodyworkComplete && !hasPaintComplete && !hasMechanicsComplete && !hasParts)
            {
                yield return new ValidationResult(
                    "Informe ao menos: funilaria completa, pintura completa, mecânica completa, ou pelo menos uma peça.",
                    [nameof(BodyworkDescription), nameof(BodyworkValue), nameof(PaintDescription), nameof(PaintValue), nameof(MechanicsDescription), nameof(MechanicsValue), nameof(Parts)]);
            }
        }
    }

    public class CreateServiceOrderPartItemRequest
    {
        [Required]
        public string Description { get; set; }

        [Range(
            typeof(decimal),
            "0.01",
            "79228162514264337593543950335",
            ParseLimitsInInvariantCulture = true,
            ConvertValueInInvariantCulture = true)]
        public decimal Quantity { get; set; }

        [Range(
            typeof(decimal),
            "0.01",
            "79228162514264337593543950335",
            ParseLimitsInInvariantCulture = true,
            ConvertValueInInvariantCulture = true)]
        public decimal UnitPrice { get; set; }
    }
}
