// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.DependencyInjection;
using Umbraco.Extensions;

namespace Umbraco.Cms.Core.PropertyEditors;

public class MultiUrlPickerValueEditor : DataValueEditor, IDataValueReference
{
    private static readonly JsonSerializerSettings _linkDisplayJsonSerializerSettings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly ILogger<MultiUrlPickerValueEditor> _logger;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;

    public MultiUrlPickerValueEditor(
        ILogger<MultiUrlPickerValueEditor> logger,
        ILocalizedTextService localizedTextService,
        IShortStringHelper shortStringHelper,
        DataEditorAttribute attribute,
        IPublishedUrlProvider publishedUrlProvider,
        IJsonSerializer jsonSerializer,
        IIOHelper ioHelper,
        IContentService contentService,
        IMediaService mediaService)
        : base(localizedTextService, shortStringHelper, jsonSerializer, ioHelper, attribute)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishedUrlProvider = publishedUrlProvider;
        _contentService = contentService;
        _mediaService = mediaService;
    }

    [Obsolete("Use non-obsolete constructor. Scheduled for removal in Umbraco 14.")]
    public MultiUrlPickerValueEditor(
        IEntityService entityService,
        IPublishedSnapshotAccessor publishedSnapshotAccessor,
        ILogger<MultiUrlPickerValueEditor> logger,
        ILocalizedTextService localizedTextService,
        IShortStringHelper shortStringHelper,
        DataEditorAttribute attribute,
        IPublishedUrlProvider publishedUrlProvider,
        IJsonSerializer jsonSerializer,
        IIOHelper ioHelper,
        IContentService contentService,
        IMediaService mediaService)
    :this(
        logger,
        localizedTextService,
        shortStringHelper,
        attribute,
        publishedUrlProvider,
        jsonSerializer,
        ioHelper,
        contentService,
        mediaService)
    {

    }

    [Obsolete("Use non-obsolete constructor. Scheduled for removal in Umbraco 14.")]
    public MultiUrlPickerValueEditor(
        IEntityService entityService,
        IPublishedSnapshotAccessor publishedSnapshotAccessor,
        ILogger<MultiUrlPickerValueEditor> logger,
        ILocalizedTextService localizedTextService,
        IShortStringHelper shortStringHelper,
        DataEditorAttribute attribute,
        IPublishedUrlProvider publishedUrlProvider,
        IJsonSerializer jsonSerializer,
        IIOHelper ioHelper)
        : this(
            logger,
            localizedTextService,
            shortStringHelper,
            attribute,
            publishedUrlProvider,
            jsonSerializer,
            ioHelper,
            StaticServiceProvider.Instance.GetRequiredService<IContentService>(),
            StaticServiceProvider.Instance.GetRequiredService<IMediaService>())
    {

    }

    public IEnumerable<UmbracoEntityReference> GetReferences(object? value)
    {
        var asString = value == null ? string.Empty : value is string str ? str : value.ToString();

        if (string.IsNullOrEmpty(asString))
        {
            yield break;
        }

        List<LinkDto>? links = JsonConvert.DeserializeObject<List<LinkDto>>(asString);
        if (links is not null)
        {
            foreach (LinkDto link in links)
            {
                // Links can be absolute links without a Udi
                if (link.Udi != null)
                {
                    yield return new UmbracoEntityReference(link.Udi);
                }
            }
        }
    }

    public override object? ToEditor(IProperty property, string? culture = null, string? segment = null)
    {
        var value = property.GetValue(culture, segment)?.ToString();

        if (string.IsNullOrEmpty(value))
        {
            return Enumerable.Empty<object>();
        }

        try
        {
            List<LinkDto>? links = JsonConvert.DeserializeObject<List<LinkDto>>(value);

            var result = new List<LinkDisplay>();
            if (links is null)
            {
                return result;
            }

            foreach (LinkDto dto in links)
            {
                var icon = "icon-link";
                string? nodeName = null;
                var published = true;
                var trashed = false;
                var url = dto.Url;

                if (dto.Udi is not null)
                {
                    if (dto.Udi.EntityType == Constants.UdiEntityType.Document)
                    {
                        IContent? content = _contentService.GetById(dto.Udi.Guid);
                        if (content is not null)
                        {
                            icon = content.ContentType.Icon;
                            nodeName = content.Name;
                            published = culture == null
                                ? content.Published
                                : content.IsCulturePublished(culture);
                            trashed = content.Trashed;
                        }

                        url = _publishedUrlProvider.GetUrl(dto.Udi.Guid, UrlMode.Relative, culture);
                    }
                    else if (dto.Udi.EntityType == Constants.UdiEntityType.Media)
                    {
                        IMedia? media = _mediaService.GetById(dto.Udi.Guid);
                        if (media is not null)
                        {
                            icon = media.ContentType.Icon;
                            nodeName = media.Name;
                            published = media.Trashed is false;
                            trashed = media.Trashed;
                        }

                        url = _publishedUrlProvider.GetMediaUrl(dto.Udi.Guid, UrlMode.Relative, culture);
                    }
                }

                result.Add(new LinkDisplay
                {
                    Icon = icon,
                    Name = dto.Name,
                    NodeName = nodeName,
                    Published = published,
                    QueryString = dto.QueryString,
                    Target = dto.Target,
                    Trashed = trashed,
                    Udi = dto.Udi,
                    Url = url,
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links");
        }

        return base.ToEditor(property, culture, segment);
    }

    public override object? FromEditor(ContentPropertyData editorValue, object? currentValue)
    {
        var value = editorValue.Value?.ToString();

        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            List<LinkDisplay>? links = JsonConvert.DeserializeObject<List<LinkDisplay>>(value);
            if (links?.Count == 0)
            {
                return null;
            }

            return JsonConvert.SerializeObject(
                from link in links
                select new LinkDto
                {
                    Name = link.Name,
                    QueryString = link.QueryString,
                    Target = link.Target,
                    Udi = link.Udi,
                    Url = link.Udi is null ? link.Url : null, // only save the URL for external links
                },
                _linkDisplayJsonSerializerSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving links");
        }

        return base.FromEditor(editorValue, currentValue);
    }

    [DataContract]
    public class LinkDto
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "target")]
        public string? Target { get; set; }

        [DataMember(Name = "udi")]
        public GuidUdi? Udi { get; set; }

        [DataMember(Name = "url")]
        public string? Url { get; set; }

        [DataMember(Name = "queryString")]
        public string? QueryString { get; set; }
    }
}
