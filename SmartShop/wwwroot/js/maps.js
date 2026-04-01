(function () {
    var mapEl = document.getElementById('map');
    if (!mapEl) return;

    var tokenMeta = document.querySelector('meta[name="mapbox-token"]');
    if (!tokenMeta || !tokenMeta.content) {
        console.warn('Mapbox: access token meta tag not found or empty.');
        return;
    }

    var LNG = 105.80790888424625;
    var LAT = 21.003481934781064;

    mapboxgl.accessToken = tokenMeta.content;

    var map = new mapboxgl.Map({
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v12',
        center: [LNG, LAT],
        zoom: 15
    });

    new mapboxgl.Marker({ color: '#03cafc' })
        .setLngLat([LNG, LAT])
        .addTo(map);

    map.addControl(new mapboxgl.NavigationControl());
})();
