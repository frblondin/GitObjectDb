// Get hierarchy and git history

{
  applications(id: "t68qfrvi5yw1") {
    ...fields
    pages {
      ...fields
      name,
      constants {
       ...fields
        embeddedResource
      }
      fields {
       ...fields
      }
    }
  }
}

fragment fields on Node {
  id
  path
  history {
    sha
    message
  }
}
