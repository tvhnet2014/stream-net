﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stream
{
    public class Activity
    {
        private const string Field_Id = "id";
        private const string Field_Actor = "actor";
        private const string Field_Verb = "verb";
        private const string Field_Object = "object";
        private const string Field_ForeignId = "foreign_id";
        private const string Field_To = "to";
        private const string Field_Target = "target";
        private const string Field_Time = "time";
        private const string Field_Activities = "activities";
        private const string Field_ActorCount = "actor_count";
        private const string Field_IsRead = "is_read";
        private const string Field_IsSeen = "is_seen";
        private const string Field_CreatedAt = "created_at";
        private const string Field_UpdatedAt = "updated_at";
        private const string Field_Group = "group";

        readonly IDictionary<string, string> _data = new Dictionary<string, string>();

        public String Id { get; private set; }

        public String Actor { get; set; }

        public String Verb { get; set; }

        public String Object { get; set; }

        public String Target { get; set; }

        public DateTime? Time { get; set; }

        [JsonProperty("foreign_id")]
        public String ForeignId { get; set; }

        public IList<String> To { get; set; }

        public T GetData<T>(String name)
        {
            if (_data.ContainsKey(name))
            {
                return JsonConvert.DeserializeObject<T>(_data[name]);
            }
            return default(T);
        }

        public void SetData<T>(String name, T data)
        {
            _data[name] = JsonConvert.SerializeObject(data);
        }

        [JsonConstructor]
        internal Activity()
        {

        }

        public Activity(String actor, String verb, String @object)
        {
            Actor = actor;
            Verb = verb;
            Object = @object;
        }

        internal String ToJson(StreamClient client)
        {
            JObject obj = new JObject(
                new JProperty(Field_Actor, this.Actor),
                new JProperty(Field_Verb, this.Verb),
                new JProperty(Field_Object, this.Object));

            if (Time.HasValue)
                obj.Add(new JProperty(Field_Time, this.Time.Value.ToString("s", System.Globalization.CultureInfo.InvariantCulture)));

            if (!String.IsNullOrWhiteSpace(ForeignId))
                obj.Add(new JProperty(Field_ForeignId, this.ForeignId));

            if (!String.IsNullOrWhiteSpace(Target))
                obj.Add(new JProperty(Field_Target, this.Target));

            if (To.SafeCount() > 0)
            {
                JArray toArray = new JArray();
                (from t in To select client.SignTo(t)).ForEach((st) =>
                {
                    toArray.Add(st);
                });
                obj.Add(new JProperty(Field_To, toArray));
            }

            if (_data.SafeCount() > 0)
            {
                _data.Keys.ForEach((k) =>
                {
                    obj.Add(new JProperty(k, _data[k]));
                });
            }

            return obj.ToString();
        }

        internal static Activity FromJson(String json)
        {
            return FromJson(JObject.Parse(json));
        }

        internal static Activity FromJson(JObject obj)
        {
            Activity activity = new Activity();
            AggregateActivity aggregateActivity = null;
            NotificationActivity notificationActivity = null;

            if (obj.Properties().Any(p => p.Name == Field_Activities))
            {
                if ((obj.Properties().Any(p => p.Name == Field_IsRead)) ||
                    (obj.Properties().Any(p => p.Name == Field_IsSeen)))
                {
                    activity = aggregateActivity = notificationActivity = new NotificationActivity();
                }
                else
                {
                    activity = aggregateActivity = new AggregateActivity();
                }
            }

            obj.Properties().ForEach((prop) =>
            {
                switch (prop.Name)
                {
                    case Field_Id: activity.Id = prop.Value.Value<String>(); break;
                    case Field_Actor: activity.Actor = prop.Value.Value<String>(); break;
                    case Field_Verb: activity.Verb = prop.Value.Value<String>(); break;
                    case Field_Object: activity.Object = prop.Value.Value<String>(); break;
                    case Field_Target: activity.Target = prop.Value.Value<String>(); break;
                    case Field_ForeignId: activity.ForeignId = prop.Value.Value<String>(); break;
                    case Field_Time: activity.Time = prop.Value.Value<DateTime>(); break;
                    case Field_To:
                        {
                            JArray array = prop.Value as JArray;
                            if ((array != null) && (array.SafeCount() > 0))
                            {
                                if (array.First.Type == JTokenType.Array)
                                {
                                    // need to take the first from each array
                                    List<string> tos = new List<string>();

                                    foreach (var child in array)
                                    {
                                        var str = child.ToObject<String[]>();
                                        tos.Add(str[0]);
                                    }

                                    activity.To = tos;
                                }
                                else
                                {
                                    activity.To = prop.Value.ToObject<String[]>().ToList();
                                }
                            }
                            else
                            {
                                activity.To = new List<String>();
                            }
                            break;
                        }
                    case Field_Activities:
                        {
                            var activities = new List<Activity>();

                            JArray array = prop.Value as JArray;
                            if ((array != null) && (array.SafeCount() > 0))
                            {
                                foreach (var child in array)
                                {
                                    var childJO = child as JObject;
                                    if (childJO != null)
                                        activities.Add(FromJson(childJO));
                                }
                            }

                            if (aggregateActivity != null)
                                aggregateActivity.Activities = activities;
                            break;
                        }
                    case Field_ActorCount:
                        {
                            if (aggregateActivity != null)
                                aggregateActivity.ActorCount = prop.Value.Value<int>();
                            break;
                        }
                    case Field_IsRead:
                        {
                            if (notificationActivity != null)
                                notificationActivity.IsRead = prop.Value.Value<bool>();
                            break;
                        }
                    case Field_IsSeen:
                        {
                            if (notificationActivity != null)
                                notificationActivity.IsSeen = prop.Value.Value<bool>();
                            break;
                        }
                    case Field_CreatedAt:
                        {
                            if (aggregateActivity != null)
                                aggregateActivity.CreatedAt = prop.Value.Value<DateTime>();
                            break;
                        }
                    case Field_UpdatedAt:
                        {
                            if (aggregateActivity != null)
                                aggregateActivity.UpdatedAt = prop.Value.Value<DateTime>();
                            break;
                        }
                    case Field_Group:
                        {
                            if (aggregateActivity != null)
                                aggregateActivity.Group = prop.Value.Value<String>();
                            break;
                        }
                    default:
                        {
                            // stash everything else as custom
                            activity._data[prop.Name] = prop.Value.ToString();
                            break;
                        };
                }
            });
            return activity;
        }
    }
}
