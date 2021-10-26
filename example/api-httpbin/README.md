# ARM Template samples showing how to Create an API Version and Revision

The example shows how to create `Versions` and `Revisions` of a sample `HttpBin` API in ApiManagement service using an apim-config.yaml and the corresponding ARM templates

## Version Set

The version set is create using this yaml section

```
apiVersionSets:
  - displayName: versionset-httpbin-api
    id: versionset-httpbin-api
    versionHeaderName: Accept-Version
    versioningScheme: Header
```

it will be created if necessary when deploying the ARM

## v1
- apim-config.yaml

Parse this file to generate the ARM template to create a new `Http Bin` Api having a `GET` and `POST` Operation defined in an online swagger  and associated to the `Starter` Product.

An custom policy is configured on the GET Operation.

## v2
- apim-config.yaml

Parse this file to generate the ARM template to create a new Version `v2` of the `Http Bin` API with another online swagger.

## v2-rev2
- apim-config.yaml

Parse this file to generate the ARM template to create a new revision of the `v2` `Http Bin` Api having which adds a `DELETE` Operation to the API.

## v2-switch-rev
- apim-config.yaml

Parse this file to generate the ARM template to create the release and switch the `HttpBinAPI-v2;rev2` to be the current Api Revision of the `v2` `HttpBin` API.

## v2-rev3
- apim-config.yaml

Parse this file to generate the ARM template to create a V2 and a  revision of the `v2` `Http Bin` using two differents protected Products (starter and unlimited)


## full
- apim-config.yaml

Parse this file to generate a full  ARM template to create :
- version set
- two protected product
- api v1 with starter product with specific policy on GET
- api v2 rev1 with starter product with specific policy on GET
- api v2 rev2 with unlimited product with specific policy on GET