/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Database;
using Nancy;
using Nancy.ModelBinding;
using System;
using UnityEngine;
using FluentValidation;
using Simulator.Database.Services;
using System.Linq;

namespace Web.Modules
{
    public class ClusterRequest
    {
        public string name;
        public string[] ips;

        public Cluster ToModel()
        {
            return new Cluster()
            {
                Name = name,
                Ips = ips == null ? string.Empty : string.Join(",", ips),
            };
        }
    }

    public class ClusterResponse
    {
        public long Id;
        public string Name;
        public string[] Ips;

        public static ClusterResponse Create(Cluster cluster)
        {
            return new ClusterResponse()
            {
                Id = cluster.Id,
                Name = cluster.Name,
                Ips = string.IsNullOrEmpty(cluster.Ips) ? Array.Empty<string>() : cluster.Ips.Split(','),
            };
        }
    }

    public class ClusterRequestValidator : AbstractValidator<ClusterRequest>
    {
        public ClusterRequestValidator()
        {
            // TODO
        }
    }

    public class ClustersModule : NancyModule
    {
        public ClustersModule(IClusterService db) : base("clusters")
        {
            Before += ctx =>
            {
                db.Open();
                return null;
            };
            After += ctx => db.Close();

            Get("/", x =>
            {
                Debug.Log($"Listing cluster");
                try
                {
                    int page = Request.Query["page"];

                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : 5;
                    return db.List(page, count).Select(ClusterResponse.Create).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to list clusters: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Get("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Getting cluster with id {id}");

                try
                {
                    var cluster = db.Get(id);
                    return ClusterResponse.Create(cluster);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Cluster with id {id} does not exist");
                    return Response.AsJson(new { error = $"Cluster with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to cluster with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/", x =>
            {
                Debug.Log($"Adding new cluster");
                try
                {
                    var req = this.BindAndValidate<ClusterRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for adding cluster failed: {message}");
                        return Response.AsJson(new { error = $"Failed to add cluster: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var cluster = req.ToModel();
                    cluster.Status = "Valid";

                    long id = db.Add(cluster);
                    Debug.Log($"Cluster added with id {id}");
                    cluster.Id = id;

                    return ClusterResponse.Create(cluster);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to add cluster: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Updating cluster with id {id}");

                try
                {
                    if (id == 0)
                    {
                        throw new Exception("Cannot edit default cluster");
                    }

                    var req = this.BindAndValidate<ClusterRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for adding cluster failed: {message}");
                        return Response.AsJson(new { error = $"Failed to add cluster: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var cluster = req.ToModel();

                    int result = db.Update(cluster);
                    if (result > 1)
                    {
                        throw new Exception($"More than one cluster has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    cluster.Status = "Valid";

                    return ClusterResponse.Create(cluster);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Cluster with id {id} does not exist");
                    return Response.AsJson(new { error = $"Cluster with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to update cluster with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Delete("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Removing cluster with id {id}");

                try
                {
                    if (id == 0)
                    {
                        throw new Exception("Cannot remove default cluster");
                    }

                    int result = db.Delete(id);
                    if (result > 1)
                    {
                        throw new Exception($"More than one cluster has id {id}");
                    }

                    if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Cluster with id {id} does not exist");
                    return Response.AsJson(new { error = $"Cluster with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to remove cluster with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });
        }
    }
}
