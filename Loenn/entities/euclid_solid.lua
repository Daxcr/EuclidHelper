local solid = {}

solid.name = "EuclidHelper/PortalSafeSolid"
solid.depth = 8998
solid.placements = {
    name = "normal",
    data = {
        width = 100,
        height = 100
    }
}
solid.nodeLimits = {1, 1}
solid.nodeLineRenderType = "line"

return solid