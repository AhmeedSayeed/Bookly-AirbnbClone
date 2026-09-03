using BLL.DTOs.Elasticsearch;
using BLL.DTOs.Listing;
using BLL.Services.Interfaces;
using DAL.Models.Common;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class ElasticListingService : IElasticListingService
    {
        private readonly IElasticClient _elasticClient;

        public ElasticListingService(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<bool> IndexListingAsync(ListingDocument document)
        {
            var response = await _elasticClient.IndexDocumentAsync(document);
            return response.IsValid;
        }

        public async Task<bool> DeleteListingAsync(int listingId)
        {
            var response = await _elasticClient.DeleteAsync<ListingDocument>(listingId);
            return response.IsValid;
        }

        public async Task<PagedResult<ListingCardDto>> SearchAsync(ListingSearchRequestDto request)
        {
            var mustClauses = new List<Func<QueryContainerDescriptor<ListingDocument>, QueryContainer>>();
            var filterClauses = new List<Func<QueryContainerDescriptor<ListingDocument>, QueryContainer>>();
            var mustNotClauses = new List<Func<QueryContainerDescriptor<ListingDocument>, QueryContainer>>();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                mustClauses.Add(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(doc => doc.Title, boost: 3)
                            .Field(doc => doc.City, boost: 2.5)
                            .Field(doc => doc.Description, boost: 1.3))
                        .Query(request.SearchTerm)
                        .Type(TextQueryType.CrossFields)
                        .MinimumShouldMatch("75%")
                    ));
            }
            else
            {
                mustClauses.Add(q => q.MatchAll());
            }

            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                filterClauses.Add(q => q
                    .Range(r => r
                        .Field(f => f.PricePerNight)
                        .GreaterThanOrEquals(request.MinPrice > 0 ? (double?)request.MinPrice : null)
                        .LessThanOrEquals(request.MaxPrice > 0 ? (double?)request.MaxPrice : null)
                    ));
            }

            ApplyRangeFilter(filterClauses, request.GuestsRange, f => f.MaxGuests);
            ApplyRangeFilter(filterClauses, request.BedroomsRange, f => f.Bedrooms);
            ApplyRangeFilter(filterClauses, request.BathroomsRange, f => f.Bathrooms);

            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                foreach (var amenityId in request.AmenityIds)
                {
                    filterClauses.Add(q => q.Term(t => t.Field(f => f.AmenityIds).Value(amenityId)));
                }
            }

            if (request.PropertyTypes != null && request.PropertyTypes.Any())
            {
                filterClauses.Add(q => q.Terms(t => t.Field(f => f.PropertyType.Suffix("keyword")).Terms(request.PropertyTypes)));
            }

            if (request.CancellationPolicies != null && request.CancellationPolicies.Any())
            {
                filterClauses.Add(q => q.Terms(t => t.Field(f => f.CancellationPolicy.Suffix("keyword")).Terms(request.CancellationPolicies)));
            }

            if (request.CheckIn.HasValue && request.CheckOut.HasValue)
            {
                var datesToCheck = new List<DateTime>();
                for (var d = request.CheckIn.Value.Date; d < request.CheckOut.Value.Date; d = d.AddDays(1))
                {
                    datesToCheck.Add(d);
                }

                mustNotClauses.Add(q => q
                    .Terms(t => t
                        .Field(f => f.UnavailableDates)
                        .Terms(datesToCheck)
                    ));
            }

            var response = await _elasticClient.SearchAsync<ListingDocument>(s =>
            {
                s.From((request.PageNumber - 1) * request.PageSize)
                 .Size(request.PageSize)
                 .Query(q => q
                     .Bool(b => b
                         .Must(mustClauses)
                         .Filter(filterClauses)
                         .MustNot(mustNotClauses)
                     )
                 );

                if (string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    s.Sort(so => so.Descending(f => f.CreatedAt));
                }

                return s;
            });

            var items = response.Documents.Select(doc => new ListingCardDto
            {
                Id = doc.Id,
                Title = doc.Title,
                City = doc.City,
                PricePerNight = (decimal)doc.PricePerNight,
                ThumbnailUrl = doc.ThumbnailUrl,
                HostName = doc.HostName,
                Latitude = doc.Latitude,
                Longitude = doc.Longitude
            }).ToList();

            return new PagedResult<ListingCardDto>
            {
                Items = items,
                TotalCount = (int)response.Total,
                PageIndex = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        private void ApplyRangeFilter(
            List<Func<QueryContainerDescriptor<ListingDocument>, QueryContainer>> clauses,
            string rangeString,
            System.Linq.Expressions.Expression<Func<ListingDocument, object>> field)
        {
            if (string.IsNullOrWhiteSpace(rangeString)) return;

            if (rangeString.Contains("+"))
            {
                var min = int.Parse(rangeString.Replace("+", ""));
                clauses.Add(q => q.Range(r => r.Field(field).GreaterThanOrEquals(min)));
            }
            else if (rangeString.Contains("-"))
            {
                var parts = rangeString.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                {
                    clauses.Add(q => q.Range(r => r.Field(field).GreaterThanOrEquals(min).LessThanOrEquals(max)));
                }
            }
        }
    }
}