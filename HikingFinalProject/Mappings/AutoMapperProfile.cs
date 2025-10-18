using AutoMapper;
using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.DTOs.Map;
using HikingFinalProject.DTOs.Mapbox;
using HikingFinalProject.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HikingFinalProject.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ==========================
            // Park ↔ ParkDto
            // ==========================
            CreateMap<Park, ParkDto>()
             .ForMember(dest => dest.ParkID, opt => opt.MapFrom(src => src.ParkID))
             .ForMember(dest => dest.ParkName, opt => opt.MapFrom(src => src.ParkName))
             .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
             .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
             .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
             .ReverseMap()
            // .ForMember(dest => dest.ParkID, opt => opt.MapFrom(src => src.ID))
             .ForMember(dest => dest.ParkName, opt => opt.MapFrom(src => src.ParkName))
             .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
             .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
             .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude));

            // ==========================
            // HikingRoute ↔ HikingRouteDto
            // ==========================
            CreateMap<HikingRoute, HikingRouteDto>()
           .ForMember(dest => dest.RouteID, opt => opt.MapFrom(src => src.RouteID))
           .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => src.RouteName))
           .ForMember(dest => dest.ParkID, opt => opt.MapFrom(src => src.ParkID))
           .ForMember(dest => dest.ParkName, opt => opt.MapFrom(src => src.Park == null ? null : src.Park.ParkName))
           .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
           .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => src.Difficulty))
           .ForMember(dest => dest.Distance, opt => opt.MapFrom(src => src.Distance))
           .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
           .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
           .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
           .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
           .ForMember(dest => dest.RecentFeedback, opt => opt.MapFrom(src => src.Feedback
               .OrderByDescending(f => f.CreatedAt).Take(3)))
           .ForMember(dest => dest.RecentImages, opt => opt.MapFrom(src => src.Images
               .OrderByDescending(i => i.CreatedAt).Take(3)))
           .ReverseMap();



            // ==========================
            // RoutePoint ↔ RoutePointDto
            // ==========================
            CreateMap<RoutePoint, RoutePointDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RouteID, opt => opt.MapFrom(src => src.RouteID))
                .ForMember(dest => dest.latitude, opt => opt.MapFrom(src => src.Latitude))
                .ForMember(dest => dest.longitude, opt => opt.MapFrom(src => src.Longitude))
                .ForMember(dest => dest.description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.time, opt => opt.MapFrom(src => src.Time))
                .ForMember(dest => dest.elevation, opt => opt.MapFrom(src => src.Elevation))
                .ForMember(dest => dest.pointOrder, opt => opt.MapFrom(src => src.PointOrder))
                .ForMember(dest => dest.isDeleted, opt => opt.MapFrom(src => src.isDeleted))
                .ForMember(dest => dest.createdAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

            // ==========================
            // RouteImage ↔ RouteImageDto
            // ==========================
            CreateMap<RouteImages, RouteImageDto>()
                .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.routeId, opt => opt.MapFrom(src => src.RouteID))
                .ForMember(dest => dest.imageURL, opt => opt.MapFrom(src => src.ImageURL))
                .ForMember(dest => dest.caption, opt => opt.MapFrom(src => src.Caption))
                .ForMember(dest => dest.fileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.filePath, opt => opt.MapFrom(src => src.FilePath))
                .ForMember(dest => dest.dateStamp, opt => opt.MapFrom(src => src.DateStamp))
                .ForMember(dest => dest.isDeleted, opt => opt.MapFrom(src => src.IsDeleted))
                .ForMember(dest => dest.createdAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.updatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
                .ReverseMap();

            // ==========================
            // RouteFeedback ↔ RouteFeedbackDto
            // ==========================
            CreateMap<RouteFeedback, RouteFeedbackDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RouteID, opt => opt.MapFrom(src => src.RouteID))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ReverseMap();

            // ==========================
            // DashboardRouteDto Mapping
            // ==========================
            CreateMap<HikingRoute, DashboardRouteDto>()
                .ForMember(dest => dest.RouteID, opt => opt.MapFrom(src => src.RouteID))
                .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => src.RouteName))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ParkName, opt => opt.Ignore()); // populated in service

            CreateMap<RouteFeedback, DashboardFeedbackDto>()
         .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
         .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
         .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
         .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
         .ForMember(dest => dest.RouteName, opt => opt.Ignore()); // fill in service
           
            
            CreateMap<RouteImages, DashboardImageDto>()
                .ForMember(dest => dest.ImageURL, opt => opt.MapFrom(src => src.ImageURL))
                .ForMember(dest => dest.Caption, opt => opt.MapFrom(src => src.Caption));





            // Note: Additional properties like RouteName or average rating should be set in service


            // ==========================
            // DashboardRouteDto ↔ MarkerRouteDto
            // ==========================
            CreateMap<DashboardRouteDto, MarkerRouteDto>()
                .ForMember(dest => dest.RouteID, opt => opt.MapFrom(src => src.RouteID))
                .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => src.RouteName))
                .ForMember(dest => dest.ImageURL, opt => opt.MapFrom(src => src.ImageURL))
               // .ForMember(dest => dest.Longitude, opt => opt.Ignore()) // adjust as needed
                .ReverseMap();

        }
    }
}