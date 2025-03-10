﻿using Foundatio.Parsers.LuceneQueries.Visitors;
using Exceptionless.Core.Repositories.Configuration;
using Microsoft.Extensions.Logging;

namespace Exceptionless.Core.Queries.Validation;

public sealed class PersistentEventQueryValidator : AppQueryValidator {
    private readonly HashSet<string> _freeQueryFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "date",
            "type",
            EventIndex.Alias.ReferenceId,
            "reference_id",
            EventIndex.Alias.OrganizationId,
            "organization_id",
            EventIndex.Alias.ProjectId,
            "project_id",
            EventIndex.Alias.StackId,
            "stack_id",
            "status"
        };

    private static readonly HashSet<string> _freeAggregationFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "date",
            "type",
            "value",
            "count",
            EventIndex.Alias.IsFirstOccurrence,
            "is_first_occurrence",
            "stack",
            EventIndex.Alias.StackId,
            EventIndex.Alias.User,
            "data.@user.identity",
            "status"
        };

    private static readonly HashSet<string> _allowedAggregationFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "date",
            "source",
            "tags",
            "type",
            "status",
            "value",
            "count",
            "geo",
            EventIndex.Alias.IsFirstOccurrence,
            "is_first_occurrence",
            EventIndex.Alias.OrganizationId,
            "organization_id",
            EventIndex.Alias.ProjectId,
            "project_id",
            EventIndex.Alias.StackId,
            "stack_id",
            "os",
            "error.code",
            "error.type",
            "error.targettype",
            "error.targetmethod",
            EventIndex.Alias.MachineArchitecture,
            "data.@environment.architecture",
            EventIndex.Alias.MachineName,
            "data.@environment.machine_name",
            EventIndex.Alias.LocationCountry,
            "data.@location.country",
            EventIndex.Alias.LocationLevel1,
            "data.@location.level1",
            EventIndex.Alias.LocationLevel2,
            "data.@location.level2",
            EventIndex.Alias.LocationLocality,
            "data.@location.locality",
            EventIndex.Alias.Browser,
            "data.@request.data.@browser",
            "data.@request.data.@browser_major_version",
            EventIndex.Alias.Device,
            "data.@request.data.@device",
            "data.@request.data.@os_version",
            "data.@request.data.@os_major_version",
            EventIndex.Alias.RequestIsBot,
            "data.@request.data.@is_bot",
            EventIndex.Alias.Version,
            "data.@version",
            EventIndex.Alias.User,
            "data.@user.identity",
            EventIndex.Alias.Level,
            "data.@level"
        };

    public PersistentEventQueryValidator(ExceptionlessElasticConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration.Events.QueryParser, loggerFactory) { }

    protected override QueryProcessResult ApplyQueryRules(QueryValidationInfo info) {
        return new QueryProcessResult {
            IsValid = info.IsValid,
            UsesPremiumFeatures = !info.ReferencedFields.All(_freeQueryFields.Contains)
        };
    }

    protected override QueryProcessResult ApplyAggregationRules(QueryValidationInfo info) {
        if (!info.IsValid)
            return new QueryProcessResult { Message = "Invalid aggregation" };

        if (info.MaxNodeDepth > 6)
            return new QueryProcessResult { Message = "Aggregation max depth exceeded" };

        if (info.Operations.Values.Sum(o => o.Count) > 10)
            return new QueryProcessResult { Message = "Aggregation count exceeded" };

        // Only allow fields that are numeric or have high commonality.
        if (!info.ReferencedFields.All(_allowedAggregationFields.Contains))
            return new QueryProcessResult { Message = "One or more aggregation fields are not allowed" };

        // Distinct queries are expensive.
        if (info.Operations.TryGetValue(AggregationType.Cardinality, out var values) && values.Count > 3)
            return new QueryProcessResult { Message = "Cardinality aggregation count exceeded" };

        // Term queries are expensive.
        if (info.Operations.TryGetValue(AggregationType.Terms, out values) && (values.Count > 3))
            return new QueryProcessResult { Message = "Terms aggregation count exceeded" };

        bool usesPremiumFeatures = !info.ReferencedFields.All(_freeAggregationFields.Contains);
        return new QueryProcessResult {
            IsValid = info.IsValid,
            UsesPremiumFeatures = usesPremiumFeatures
        };
    }
}
