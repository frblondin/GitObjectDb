// Get hierarchy and git history

{
  applications(id: "t68qfrvi5yw1") {
    ...fields
    pages {
      ...fields
      name,
      constants {
       ...fields
        value
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
