# Initialization through GraphQL

```
mutation {
  siteType: createOrganizationType(node: {
    id: "site", label: "Site"
  })
  regionType: createOrganizationType(node: {
    id: "region", label: "Region"
  })
  zoneType: createOrganizationType(node: {
    id: "zone", label: "Zone"
  })
  
  siteX: createOrganization(node: {
    id: "siteX"
    label: "Site X"
    type: "Types/site.json"
    timeZone: "UTC;0;(UTC) Coordinated Universal Time;Coordinated Universal Time;Coordinated Universal Time;;"
  })
  
  na: createOrganization(node: {
    id: "northAmerica"
    label: "NorthAmerica"
    type: "Types/region.json"
  })
  usa: createOrganization(node: {
    id: "usa"
    label: "United States of America"
    type: "Types/region.json"
  }, parentId: "northAmerica")
  alfa: createOrganization(node: {
    id: "alfa"
    label: "Site Alfa"
    type: "Types/zone.json"
  }, parentId: "usa")
  beta: createOrganization(node: {
    id: "beta"
    label: "Site Beta"
    type: "Types/zone.json"
  }, parentId: "usa")
  canada: createOrganization(node: {
    id: "canada"
    label: "Canada"
    type: "Types/region.json"
  }, parentId: "northAmerica")

  europe: createOrganization(node: {
    id: "europe"
    label: "Europe"
    type: "Types/region.json"
  })

  initialCommit: commit(
    message: "Initial commit",
    author: "Me",
    email: "me@myself.com"
  )
  
  updateAlfa: createOrganization(node: {
    id: "alfa"
    label: "Site Alfa"
    type: "Types/zone.json"
    graphicalOrganizationStructureId: "hahaha"
  }, parentId: "usa")
  updateBeta: createOrganization(node: {
    id: "beta"
    label: "Site Beta"
    type: "Types/zone.json"
    graphicalOrganizationStructureId: "huhuhu"
  }, parentId: "usa")
  
  updateCommit: commit(
    message: "Update GOS ids",
    author: "Me",
    email: "me@myself.com"
  )
}
```

# Get delta

```
{
  organizationsDelta(start: "HEAD~1") {
    updatedAt
    deleted
    old {
      label
      graphicalOrganizationStructureId
      timeZone
      embeddedResource
      path
    }
    new {
      label
      graphicalOrganizationStructureId
      timeZone
      embeddedResource
      path
    }
  }
}
```
